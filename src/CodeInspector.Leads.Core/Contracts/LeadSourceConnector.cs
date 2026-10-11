namespace CodeInspector.Leads.Core.Contracts;

public record LeadImportInput(
    Guid UserId,
    string SourceType,
    string CollectionMethod,
    string? SourceUrl,
    string? PermittedUseNotes,
    IReadOnlyDictionary<string, string>? Evidence,
    ManualLeadPayload? ManualLead = null,
    Stream? CsvStream = null,
    string? CsvFileName = null);

public record ManualLeadPayload(
    string CompanyName,
    string? CompanyWebsite,
    string? Country,
    string? Industry,
    string? EmployeeRange,
    IReadOnlyList<string>? TechnologySignals,
    string FullName,
    string? JobTitle,
    string? LinkedInProfileUrl,
    string? BusinessEmail,
    string? Notes);

public record LeadImportRowResult(
    int RowNumber,
    bool Imported,
    bool Duplicate,
    Guid? LeadId,
    Guid? CompanyId,
    string? Error);

public record LeadImportResult(
    Guid BatchId,
    int TotalRows,
    int ImportedRows,
    int DuplicateRows,
    int RejectedRows,
    IReadOnlyList<LeadImportRowResult> RowResults);

public interface ILeadSourceConnector
{
    string SourceName { get; }
    Task<LeadImportResult> ImportAsync(LeadImportInput input, CancellationToken cancellationToken);
}
