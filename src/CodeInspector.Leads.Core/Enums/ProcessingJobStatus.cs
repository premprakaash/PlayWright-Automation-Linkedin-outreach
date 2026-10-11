namespace CodeInspector.Leads.Core.Enums;

public enum ProcessingJobStatus
{
    Pending = 0,
    Claimed = 1,
    Running = 2,
    Completed = 3,
    Failed = 4,
    Skipped = 5,
    NeedsManualReview = 6
}
