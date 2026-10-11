using CodeInspector.Leads.Core.Contracts;
using CodeInspector.Leads.Core.Entities;
using CodeInspector.Leads.Core.Enums;
using CodeInspector.Leads.Infrastructure.Data;
using CodeInspector.Leads.Infrastructure.Services;

namespace CodeInspector.Leads.Infrastructure.Connectors;

public class ManualLeadConnector : ILeadSourceConnector
{
    private readonly LeadPersistenceService _persistence;
    private readonly LeadsDbContext _db;

    public ManualLeadConnector(LeadPersistenceService persistence, LeadsDbContext db)
    {
        _persistence = persistence;
        _db = db;
    }

    public string SourceName => "manual";

    public async Task<LeadImportResult> ImportAsync(LeadImportInput input, CancellationToken cancellationToken)
    {
        if (input.ManualLead is null)
        {
            throw new InvalidOperationException("Manual lead payload is required.");
        }

        var batch = new ImportBatch
        {
            Id = Guid.NewGuid(),
            UserId = input.UserId,
            FileName = null,
            Status = ImportBatchStatus.Processing,
            TotalRows = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.ImportBatches.Add(batch);
        await _db.SaveChangesAsync(cancellationToken);

        var (lead, _, duplicate, error) = await _persistence.UpsertManualLeadAsync(
            input.UserId,
            input.ManualLead,
            input.SourceType,
            input.CollectionMethod,
            input.SourceUrl,
            input.Evidence?.GetValueOrDefault("notes"),
            cancellationToken);

        var rowResult = new LeadImportRowResult(1, lead is not null && error is null, duplicate, lead?.Id, lead?.CompanyId, error);
        batch.ImportedRows = rowResult.Imported && !duplicate ? 1 : 0;
        batch.DuplicateRows = duplicate ? 1 : 0;
        batch.RejectedRows = error is not null ? 1 : 0;
        if (error is not null)
        {
            batch.RowErrors.Add(new ImportRowError { RowNumber = 1, Message = error });
        }

        batch.Status = batch.RejectedRows > 0 ? ImportBatchStatus.PartiallyCompleted : ImportBatchStatus.Completed;
        batch.CompletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new LeadImportResult(batch.Id, 1, batch.ImportedRows, batch.DuplicateRows, batch.RejectedRows, [rowResult]);
    }
}
