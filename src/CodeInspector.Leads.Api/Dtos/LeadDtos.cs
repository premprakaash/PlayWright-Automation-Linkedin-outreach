using System.ComponentModel.DataAnnotations;
using CodeInspector.Leads.Core.Enums;

namespace CodeInspector.Leads.Api.Dtos;

public record CreateResearchSessionRequest([Required, MaxLength(500)] string Keywords);

public record ResearchSessionResponse(
    Guid Id,
    string Keywords,
    string SearchUrl,
    string Status,
    DateTimeOffset OpenedAt,
    DateTimeOffset? CompletedAt,
    string? Notes);

public record CreateCompanyRequest(
    [Required, MaxLength(500)] string Name,
    string? Website,
    string? Industry,
    string? EmployeeRange,
    string? Country,
    List<string>? TechnologySignals);

public record CompanyResponse(
    Guid Id,
    string Name,
    string? Website,
    string? Industry,
    string? EmployeeRange,
    string? Country,
    List<string> TechnologySignals,
    List<string> Evidence,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record PatchCompanyRequest(
    string? Name,
    string? Website,
    string? Industry,
    string? EmployeeRange,
    string? Country,
    List<string>? TechnologySignals,
    CompanyStatus? Status);

public record CreateLeadRequest(
    [Required] Guid CompanyId,
    Guid? ResearchSessionId,
    [Required, MaxLength(500)] string FullName,
    string? JobTitle,
    string? LinkedInProfileUrl,
    string? BusinessEmail,
    string? Notes);

public record LeadResponse(
    Guid Id,
    Guid CompanyId,
    string FullName,
    string? JobTitle,
    string? LinkedInProfileUrl,
    string? BusinessEmail,
    int FitScore,
    object ScoreReasons,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    CompanyResponse? Company);

public record PatchLeadRequest(
    string? FullName,
    string? JobTitle,
    string? LinkedInProfileUrl,
    string? BusinessEmail,
    LeadStatus? Status);

public record CreateDraftRequest(OutreachChannel Channel);

public record DraftResponse(
    Guid Id,
    Guid LeadId,
    string Channel,
    string TemplateVersion,
    string? Subject,
    string Body,
    Dictionary<string, string> EvidenceUsed,
    string Status,
    DateTimeOffset CreatedAt);

public record PatchDraftRequest(string Body, string? Subject);

public record CreateActivityRequest(OutreachActivityType ActivityType, Guid? DraftId, string? Notes);

public record SuppressLeadRequest([Required] string Reason);

public record DashboardSummaryResponse(
    int TotalCompanies,
    int TotalLeads,
    int QualifiedLeads,
    int DraftsAwaitingReview,
    int MessagesSent,
    int Replies,
    int PositiveReplies,
    int DemosBooked,
    int Conversions,
    object? LatestProcessingRun,
    int FailedJobs);

public record TokenRequest([Required] Guid UserId, bool Admin = false);

public record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt);
