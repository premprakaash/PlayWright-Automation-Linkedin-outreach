using CodeInspector.Leads.Core.Enums;

namespace CodeInspector.Leads.Core.Entities;

public class ResearchSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Keywords { get; set; } = string.Empty;
    public string SearchUrl { get; set; } = string.Empty;
    public ResearchSessionStatus Status { get; set; } = ResearchSessionStatus.Created;
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? Notes { get; set; }

    public ICollection<Lead> Leads { get; set; } = [];
}
