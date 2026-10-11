using CodeInspector.Leads.Core.Enums;

namespace CodeInspector.Leads.Core.Entities;

public class Company
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? NormalizedWebsite { get; set; }
    public string? Industry { get; set; }
    public string? EmployeeRange { get; set; }
    public string? Country { get; set; }
    public List<string> TechnologySignals { get; set; } = [];
    public List<string> Evidence { get; set; } = [];
    public CompanyStatus Status { get; set; } = CompanyStatus.Candidate;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Lead> Leads { get; set; } = [];
    public ICollection<LeadSource> Sources { get; set; } = [];
    public ICollection<ProcessingJob> ProcessingJobs { get; set; } = [];
}
