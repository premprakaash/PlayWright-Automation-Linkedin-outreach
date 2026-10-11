using CodeInspector.Leads.Core.Enums;

namespace CodeInspector.Leads.Core.Entities;

public class ProcessingJob
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public string RunKey { get; set; } = string.Empty;
    public ProcessingJobStatus Status { get; set; } = ProcessingJobStatus.Pending;
    public int Attempts { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Company Company { get; set; } = null!;
}
