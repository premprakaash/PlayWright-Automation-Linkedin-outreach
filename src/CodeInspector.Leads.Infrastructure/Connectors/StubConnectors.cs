using CodeInspector.Leads.Core.Contracts;

namespace CodeInspector.Leads.Infrastructure.Connectors;

public class ApprovedProviderConnector : ILeadSourceConnector
{
    public string SourceName => "approved_provider";

    public Task<LeadImportResult> ImportAsync(LeadImportInput input, CancellationToken cancellationToken) =>
        Task.FromException<LeadImportResult>(new NotImplementedException("Approved provider integration is not yet implemented."));
}

public class CompanyWebsiteConnector : ILeadSourceConnector
{
    public string SourceName => "company_website";

    public Task<LeadImportResult> ImportAsync(LeadImportInput input, CancellationToken cancellationToken) =>
        Task.FromException<LeadImportResult>(new NotImplementedException("Use the daily worker company research browser instead."));
}

public class GitHubOrganizationConnector : ILeadSourceConnector
{
    public string SourceName => "github_organization";

    public Task<LeadImportResult> ImportAsync(LeadImportInput input, CancellationToken cancellationToken) =>
        Task.FromException<LeadImportResult>(new NotImplementedException("GitHub organization connector is not yet implemented."));
}
