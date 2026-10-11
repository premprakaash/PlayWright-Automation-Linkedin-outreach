namespace CodeInspector.Leads.Core.Contracts;

public record CompanyResearchResult(
    bool Success,
    bool NeedsManualReview,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> TechnologySignals,
    string? Error);

public interface ICompanyResearchBrowser
{
    Task<CompanyResearchResult> ResearchCompanyWebsiteAsync(
        Guid userId,
        string normalizedWebsite,
        CancellationToken cancellationToken);
}
