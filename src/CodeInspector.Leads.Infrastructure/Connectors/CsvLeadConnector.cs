using System.Globalization;
using System.Text;
using CodeInspector.Leads.Core.Contracts;
using CodeInspector.Leads.Core.Entities;
using CodeInspector.Leads.Core.Enums;
using CodeInspector.Leads.Infrastructure.Data;
using CodeInspector.Leads.Infrastructure.Services;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Options;

namespace CodeInspector.Leads.Infrastructure.Connectors;

public class CsvImportOptions
{
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
    public int MaxRows { get; set; } = 10_000;
}

public sealed class CsvLeadRow
{
    public string company_name { get; set; } = string.Empty;
    public string? company_website { get; set; }
    public string? country { get; set; }
    public string? industry { get; set; }
    public string? employee_range { get; set; }
    public string? technology_signals { get; set; }
    public string full_name { get; set; } = string.Empty;
    public string? job_title { get; set; }
    public string? linkedin_profile_url { get; set; }
    public string? business_email { get; set; }
    public string? source_type { get; set; }
    public string? source_url { get; set; }
    public string? notes { get; set; }
}

public class CsvLeadConnector : ILeadSourceConnector
{
    private static readonly string[] RequiredHeaders =
    [
        "company_name", "company_website", "country", "industry", "employee_range", "technology_signals",
        "full_name", "job_title", "linkedin_profile_url", "business_email", "source_type", "source_url", "notes"
    ];

    private readonly LeadPersistenceService _persistence;
    private readonly LeadsDbContext _db;
    private readonly CsvImportOptions _options;

    public CsvLeadConnector(LeadPersistenceService persistence, LeadsDbContext db, IOptions<CsvImportOptions> options)
    {
        _persistence = persistence;
        _db = db;
        _options = options.Value;
    }

    public string SourceName => "csv";

    public async Task<LeadImportResult> ImportAsync(LeadImportInput input, CancellationToken cancellationToken)
    {
        if (input.CsvStream is null)
        {
            throw new InvalidOperationException("CSV stream is required.");
        }

        if (input.CsvStream.CanSeek && input.CsvStream.Length > _options.MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"CSV exceeds maximum size of {_options.MaxFileSizeBytes} bytes.");
        }

        var batch = new ImportBatch
        {
            Id = Guid.NewGuid(),
            UserId = input.UserId,
            FileName = input.CsvFileName,
            Status = ImportBatchStatus.Processing,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.ImportBatches.Add(batch);
        await _db.SaveChangesAsync(cancellationToken);

        var rowResults = new List<LeadImportRowResult>();
        using var reader = new StreamReader(input.CsvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(reader, config);
        if (!await csv.ReadAsync() || !csv.ReadHeader())
        {
            batch.Status = ImportBatchStatus.Failed;
            batch.CompletedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("CSV header row is missing.");
        }

        var headers = csv.HeaderRecord ?? Array.Empty<string>();
        foreach (var required in RequiredHeaders)
        {
            if (!headers.Any(h => string.Equals(h, required, StringComparison.OrdinalIgnoreCase)))
            {
                batch.Status = ImportBatchStatus.Failed;
                batch.CompletedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                throw new InvalidOperationException($"Missing required CSV column: {required}");
            }
        }

        var rowNumber = 1;
        while (await csv.ReadAsync())
        {
            rowNumber++;
            if (rowNumber - 1 > _options.MaxRows)
            {
                rowResults.Add(new LeadImportRowResult(rowNumber, false, false, null, null, "Row limit exceeded."));
                batch.RejectedRows++;
                break;
            }

            var row = csv.GetRecord<CsvLeadRow>();
            row = SanitizeRow(row);
            if (string.IsNullOrWhiteSpace(row.company_name) || string.IsNullOrWhiteSpace(row.full_name))
            {
                rowResults.Add(new LeadImportRowResult(rowNumber, false, false, null, null, "company_name and full_name are required."));
                batch.RejectedRows++;
                continue;
            }

            var signals = (row.technology_signals ?? string.Empty)
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            var payload = new ManualLeadPayload(
                row.company_name,
                row.company_website,
                row.country,
                row.industry,
                row.employee_range,
                signals,
                row.full_name,
                row.job_title,
                row.linkedin_profile_url,
                row.business_email,
                row.notes);

            var sourceType = string.IsNullOrWhiteSpace(row.source_type) ? input.SourceType : row.source_type!;
            var (lead, _, duplicate, error) = await _persistence.UpsertManualLeadAsync(
                input.UserId,
                payload,
                sourceType,
                input.CollectionMethod,
                row.source_url ?? input.SourceUrl,
                row.notes,
                cancellationToken);

            if (error is not null)
            {
                rowResults.Add(new LeadImportRowResult(rowNumber, false, false, null, null, error));
                batch.RejectedRows++;
                batch.RowErrors.Add(new ImportRowError { RowNumber = rowNumber, Message = error });
            }
            else if (duplicate)
            {
                rowResults.Add(new LeadImportRowResult(rowNumber, true, true, lead?.Id, lead?.CompanyId, null));
                batch.DuplicateRows++;
            }
            else
            {
                rowResults.Add(new LeadImportRowResult(rowNumber, true, false, lead?.Id, lead?.CompanyId, null));
                batch.ImportedRows++;
            }
        }

        batch.TotalRows = rowNumber - 1;
        batch.Status = batch.RejectedRows > 0
            ? ImportBatchStatus.PartiallyCompleted
            : ImportBatchStatus.Completed;
        batch.CompletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new LeadImportResult(batch.Id, batch.TotalRows, batch.ImportedRows, batch.DuplicateRows, batch.RejectedRows, rowResults);
    }

    private static CsvLeadRow SanitizeRow(CsvLeadRow row)
    {
        static string? Sanitize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var trimmed = value.Trim();
            if (trimmed.StartsWith('=') || trimmed.StartsWith('+') || trimmed.StartsWith('-') || trimmed.StartsWith('@'))
            {
                return "'" + trimmed;
            }

            return trimmed;
        }

        row.company_name = Sanitize(row.company_name) ?? string.Empty;
        row.full_name = Sanitize(row.full_name) ?? string.Empty;
        row.company_website = Sanitize(row.company_website);
        row.country = Sanitize(row.country);
        row.industry = Sanitize(row.industry);
        row.employee_range = Sanitize(row.employee_range);
        row.technology_signals = Sanitize(row.technology_signals);
        row.job_title = Sanitize(row.job_title);
        row.linkedin_profile_url = Sanitize(row.linkedin_profile_url);
        row.business_email = Sanitize(row.business_email);
        row.source_type = Sanitize(row.source_type);
        row.source_url = Sanitize(row.source_url);
        row.notes = Sanitize(row.notes);
        return row;
    }

    public static string EscapeForExport(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.StartsWith('=') || value.StartsWith('+') || value.StartsWith('-') || value.StartsWith('@') || value.Contains(',') || value.Contains('"'))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }
}
