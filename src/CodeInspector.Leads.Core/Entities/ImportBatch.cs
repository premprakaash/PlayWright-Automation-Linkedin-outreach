using CodeInspector.Leads.Core.Enums;

namespace CodeInspector.Leads.Core.Entities;

public class ImportBatch
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? FileName { get; set; }
    public ImportBatchStatus Status { get; set; } = ImportBatchStatus.Pending;
    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public int DuplicateRows { get; set; }
    public int RejectedRows { get; set; }
    public List<ImportRowError> RowErrors { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class ImportRowError
{
    public int RowNumber { get; set; }
    public string Message { get; set; } = string.Empty;
}
