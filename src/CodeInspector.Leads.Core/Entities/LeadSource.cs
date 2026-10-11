namespace CodeInspector.Leads.Core.Entities;

public class LeadSource
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? CompanyId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public string CollectionMethod { get; set; } = string.Empty;
    public DateTimeOffset CollectedAt { get; set; }
    public string? PermittedUseNotes { get; set; }
    public Dictionary<string, string> Evidence { get; set; } = new();

    public Lead? Lead { get; set; }
    public Company? Company { get; set; }
}
