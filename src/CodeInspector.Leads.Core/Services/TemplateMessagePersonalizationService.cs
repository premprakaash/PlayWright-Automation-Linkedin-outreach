using CodeInspector.Leads.Core.Contracts;
using CodeInspector.Leads.Core.Entities;

namespace CodeInspector.Leads.Core.Services;

public class TemplateMessagePersonalizationService : IMessagePersonalizationService
{
    public MessagePersonalizationResult Generate(Lead lead, Company company, string templateVersion = "v1")
    {
        var evidence = new Dictionary<string, string>();
        var missing = new List<string>();

        var firstName = lead.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? lead.FullName;
        if (!string.IsNullOrWhiteSpace(firstName))
        {
            evidence["first_name"] = firstName;
        }
        else
        {
            missing.Add("Contact first name");
        }

        if (!string.IsNullOrWhiteSpace(company.Name))
        {
            evidence["company_name"] = company.Name;
        }
        else
        {
            missing.Add("Company name");
        }

        if (!string.IsNullOrWhiteSpace(lead.JobTitle))
        {
            evidence["job_title"] = lead.JobTitle;
        }

        var techFact = company.TechnologySignals.FirstOrDefault() ?? company.Evidence.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(techFact))
        {
            evidence["technology_fact"] = techFact;
        }

        var companyRef = evidence.TryGetValue("company_name", out var cn) ? cn : "your team";
        var greetingName = evidence.TryGetValue("first_name", out var fn) ? fn : "there";

        var body = $"""
Hi {greetingName},

I came across {companyRef} while researching software engineering teams.

I'm building CodeInspector, a code-quality analysis platform designed to help engineering teams identify code issues and improve maintainability.

Would you be open to taking a look?

Thanks!
""";

        return new MessagePersonalizationResult(body, evidence, missing, templateVersion);
    }
}
