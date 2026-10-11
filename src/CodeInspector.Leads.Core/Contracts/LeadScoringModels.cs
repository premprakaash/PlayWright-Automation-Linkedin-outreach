using CodeInspector.Leads.Core.Entities;

namespace CodeInspector.Leads.Core.Contracts;

public record LeadScoringResult(
    int Score,
    IReadOnlyList<ScoreReasonEntry> Reasons,
    IReadOnlyList<string> MissingEvidence,
    string ScoringVersion,
    DateTimeOffset ScoredAt);

public interface ILeadScoringService
{
    LeadScoringResult Score(Company company, Lead lead, string scoringVersion = "v1");
}
