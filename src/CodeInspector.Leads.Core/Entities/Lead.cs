using CodeInspector.Leads.Core.Enums;

namespace CodeInspector.Leads.Core.Entities;

public class Lead
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? ResearchSessionId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? LinkedInProfileUrl { get; set; }
    public string? NormalizedProfileUrl { get; set; }
    public string? BusinessEmail { get; set; }
    public int FitScore { get; set; }
    public List<ScoreReasonEntry> ScoreReasons { get; set; } = [];
    public DateTimeOffset? LastVerifiedAt { get; set; }
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Company Company { get; set; } = null!;
    public ResearchSession? ResearchSession { get; set; }
    public ICollection<LeadSource> Sources { get; set; } = [];
    public ICollection<MessageDraft> Drafts { get; set; } = [];
    public ICollection<OutreachActivity> Activities { get; set; } = [];
}

public class ScoreReasonEntry
{
    public string Rule { get; set; } = string.Empty;
    public int Points { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public bool Verified { get; set; }
}
