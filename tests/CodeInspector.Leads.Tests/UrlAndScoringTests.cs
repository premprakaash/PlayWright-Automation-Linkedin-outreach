using CodeInspector.Leads.Core.Entities;
using CodeInspector.Leads.Core.Normalization;
using CodeInspector.Leads.Core.Services;
using CodeInspector.Leads.Infrastructure.Connectors;

namespace CodeInspector.Leads.Tests;

public class UrlAndScoringTests
{
    [Fact]
    public void LinkedInSearchUrl_EncodesKeywords()
    {
        var url = LinkedInSearchUrlBuilder.BuildPeopleSearchUrl("CTO .NET SaaS");
        Assert.Contains("keywords=CTO%20.NET%20SaaS", url);
        Assert.StartsWith("https://www.linkedin.com/search/results/people/", url);
    }

    [Fact]
    public void LinkedInProfile_NormalizesHttpsAndRemovesQuery()
    {
        var ok = UrlNormalizer.TryNormalizeLinkedInProfileUrl(
            "https://www.linkedin.com/in/jane-doe/?utm=1#fragment", out var normalized, out var error);
        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal("https://www.linkedin.com/in/jane-doe", normalized);
    }

    [Fact]
    public void LinkedInProfile_RejectsNonLinkedInHost()
    {
        var ok = UrlNormalizer.TryNormalizeLinkedInProfileUrl("https://evil.com/in/jane", out _, out var error);
        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void Website_NormalizesWwwAndTrailingSlash()
    {
        var ok = UrlNormalizer.TryNormalizeWebsite("https://WWW.Example.com/path/", out var normalized, out _);
        Assert.True(ok);
        Assert.Equal("https://example.com/path", normalized);
    }

    [Fact]
    public void Scoring_AwardsVerifiedDotNetSaaSAndDecisionMaker()
    {
        var scoring = new LeadScoringService();
        var company = new Company
        {
            Name = "Acme",
            Industry = "SaaS software product",
            TechnologySignals = [".NET", "C#"],
            EmployeeRange = "11-50",
            Evidence = ["engineering activity on GitHub"]
        };
        var lead = new Lead
        {
            FullName = "Jane Doe",
            JobTitle = "CTO",
            LastVerifiedAt = DateTimeOffset.UtcNow
        };

        var result = scoring.Score(company, lead);
        Assert.InRange(result.Score, 80, 100);
        Assert.Contains(result.Reasons, r => r.Rule == "dotnet_stack");
    }

    [Fact]
    public void CsvExport_EscapesFormulaInjection()
    {
        var escaped = CsvLeadConnector.EscapeForExport("=1+1");
        Assert.StartsWith("\"", escaped);
    }

    [Fact]
    public void MessageTemplate_DoesNotInventCompanyWhenMissing()
    {
        var service = new TemplateMessagePersonalizationService();
        var lead = new Lead { FullName = "Jane Doe", JobTitle = "CTO", LastVerifiedAt = DateTimeOffset.UtcNow };
        var company = new Company { Name = "Acme SaaS", TechnologySignals = [".NET"] };
        var message = service.Generate(lead, company);
        Assert.Contains("Jane", message.Body);
        Assert.Contains("Acme SaaS", message.Body);
        Assert.Contains("CodeInspector", message.Body);
    }
}
