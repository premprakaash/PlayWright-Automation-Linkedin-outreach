using CodeInspector.Leads.Core.Contracts;
using CodeInspector.Leads.Core.Entities;

namespace CodeInspector.Leads.Core.Services;

public class LeadScoringService : ILeadScoringService
{
    public LeadScoringResult Score(Company company, Lead lead, string scoringVersion = "v1")
    {
        var reasons = new List<ScoreReasonEntry>();
        var missing = new List<string>();
        var score = 0;

        if (HasVerifiedSignal(company, "saas", "software product", "software"))
        {
            Add(reasons, ref score, "saas_company", 20, "Software product or SaaS company evidence.", true);
        }
        else
        {
            missing.Add("SaaS or software product evidence");
        }

        if (HasDotNetEvidence(company))
        {
            Add(reasons, ref score, "dotnet_stack", 25, "Verified .NET/C# technology signals.", true);
        }
        else
        {
            missing.Add(".NET/C# company evidence");
        }

        if (IsDecisionMaker(lead.JobTitle))
        {
            Add(reasons, ref score, "decision_maker", 20, "Relevant decision-maker role.", lead.LastVerifiedAt.HasValue);
        }
        else
        {
            missing.Add("Decision-maker job title");
        }

        if (IsTargetEmployeeRange(company.EmployeeRange))
        {
            Add(reasons, ref score, "employee_range", 15, "Target employee range (11–200).", !string.IsNullOrWhiteSpace(company.EmployeeRange));
        }

        if (HasVerifiedSignal(company, "engineering activity", "github", "open source"))
        {
            Add(reasons, ref score, "engineering_activity", 10, "Verified engineering activity.", true);
        }

        if (lead.LastVerifiedAt.HasValue && !string.IsNullOrWhiteSpace(company.Name) && !string.IsNullOrWhiteSpace(lead.JobTitle))
        {
            Add(reasons, ref score, "verified_contact", 10, "Verified company and role.", true);
        }

        score = Math.Clamp(score, 0, 100);
        return new LeadScoringResult(score, reasons, missing, scoringVersion, DateTimeOffset.UtcNow);
    }

    private static void Add(List<ScoreReasonEntry> reasons, ref int score, string rule, int points, string explanation, bool verified)
    {
        if (!verified)
        {
            return;
        }

        score += points;
        reasons.Add(new ScoreReasonEntry
        {
            Rule = rule,
            Points = points,
            Explanation = explanation,
            Verified = true
        });
    }

    private static bool HasVerifiedSignal(Company company, params string[] needles)
    {
        var haystack = string.Join(' ',
            (company.Industry ?? string.Empty),
            string.Join(' ', company.TechnologySignals),
            string.Join(' ', company.Evidence)).ToLowerInvariant();
        return needles.Any(n => haystack.Contains(n, StringComparison.Ordinal));
    }

    private static bool HasDotNetEvidence(Company company)
    {
        return company.TechnologySignals.Any(s =>
                s.Contains(".net", StringComparison.OrdinalIgnoreCase) ||
                s.Contains("c#", StringComparison.OrdinalIgnoreCase) ||
                s.Contains("dotnet", StringComparison.OrdinalIgnoreCase))
            || company.Evidence.Any(e =>
                e.Contains(".net", StringComparison.OrdinalIgnoreCase) ||
                e.Contains("c#", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsDecisionMaker(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        var t = title.ToLowerInvariant();
        string[] roles = ["cto", "chief technology", "vp engineering", "head of engineering", "engineering manager", "founder", "co-founder", "technical director", "director of engineering"];
        return roles.Any(r => t.Contains(r, StringComparison.Ordinal));
    }

    private static bool IsTargetEmployeeRange(string? range)
    {
        if (string.IsNullOrWhiteSpace(range))
        {
            return false;
        }

        var r = range.Replace("–", "-", StringComparison.Ordinal).Trim();
        if (r is "11-50" or "51-200" or "11-200" or "11-100" or "50-200")
        {
            return true;
        }

        if (int.TryParse(r.Split('-').FirstOrDefault(), out var min) && min >= 11 && min <= 200)
        {
            return true;
        }

        return false;
    }
}
