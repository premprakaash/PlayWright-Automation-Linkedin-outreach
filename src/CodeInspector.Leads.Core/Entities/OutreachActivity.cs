using CodeInspector.Leads.Core.Enums;

namespace CodeInspector.Leads.Core.Entities;

public class OutreachActivity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid LeadId { get; set; }
    public Guid? DraftId { get; set; }
    public OutreachActivityType ActivityType { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset OccurredAt { get; set; }

    public Lead Lead { get; set; } = null!;
    public MessageDraft? Draft { get; set; }
}
