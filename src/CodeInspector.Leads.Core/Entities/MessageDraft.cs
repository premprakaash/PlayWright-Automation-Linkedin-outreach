using CodeInspector.Leads.Core.Enums;

namespace CodeInspector.Leads.Core.Entities;

public class MessageDraft
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid LeadId { get; set; }
    public OutreachChannel Channel { get; set; } = OutreachChannel.LinkedIn;
    public string TemplateVersion { get; set; } = "v1";
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string> EvidenceUsed { get; set; } = new();
    public MessageDraftStatus Status { get; set; } = MessageDraftStatus.Draft;
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Lead Lead { get; set; } = null!;
}
