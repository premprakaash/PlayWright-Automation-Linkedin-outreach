using CodeInspector.Leads.Core.Entities;

namespace CodeInspector.Leads.Core.Contracts;

public record MessagePersonalizationResult(
    string Body,
    Dictionary<string, string> EvidenceUsed,
    IReadOnlyList<string> MissingFacts,
    string TemplateVersion);

public interface IMessagePersonalizationService
{
    MessagePersonalizationResult Generate(Lead lead, Company company, string templateVersion = "v1");
}
