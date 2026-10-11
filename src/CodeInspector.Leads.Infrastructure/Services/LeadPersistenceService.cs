using CodeInspector.Leads.Core.Contracts;
using CodeInspector.Leads.Core.Entities;
using CodeInspector.Leads.Core.Enums;
using CodeInspector.Leads.Core.Normalization;
using CodeInspector.Leads.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CodeInspector.Leads.Infrastructure.Services;

public class LeadPersistenceService
{
    private readonly LeadsDbContext _db;
    private readonly LeadDeduplicationService _dedup;
    private readonly ILeadScoringService _scoring;

    public LeadPersistenceService(LeadsDbContext db, LeadDeduplicationService dedup, ILeadScoringService scoring)
    {
        _db = db;
        _dedup = dedup;
        _scoring = scoring;
    }

    public async Task<(Lead? Lead, Company Company, bool Duplicate, string? Error)> UpsertManualLeadAsync(
        Guid userId,
        ManualLeadPayload payload,
        string sourceType,
        string collectionMethod,
        string? sourceUrl,
        string? notes,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload.CompanyName) || string.IsNullOrWhiteSpace(payload.FullName))
        {
            return (null, null!, false, "Company name and full name are required.");
        }

        string? normalizedWebsite = null;
        if (!string.IsNullOrWhiteSpace(payload.CompanyWebsite))
        {
            if (!UrlNormalizer.TryNormalizeWebsite(payload.CompanyWebsite, out normalizedWebsite, out var webErr))
            {
                return (null, null!, false, webErr);
            }
        }

        string? normalizedProfile = null;
        if (!string.IsNullOrWhiteSpace(payload.LinkedInProfileUrl))
        {
            if (!UrlNormalizer.TryNormalizeLinkedInProfileUrl(payload.LinkedInProfileUrl, out normalizedProfile, out var liErr))
            {
                return (null, null!, false, liErr);
            }
        }

        var dedup = await _dedup.ResolveAsync(userId, payload.CompanyName, payload.CompanyWebsite, payload.Country,
            payload.FullName, payload.LinkedInProfileUrl, payload.BusinessEmail, cancellationToken);

        if (dedup.Outcome == DeduplicationOutcome.AmbiguousCompany)
        {
            return (null, null!, false, dedup.Message);
        }

        if (dedup.Outcome == DeduplicationOutcome.DuplicateLead && dedup.ExistingLead is not null && dedup.Company is not null)
        {
            var incomingLead = BuildLead(userId, dedup.Company.Id, payload, normalizedProfile);
            LeadDeduplicationService.MergeMissingFields(dedup.ExistingLead, incomingLead);
            await AddSourceAsync(userId, dedup.ExistingLead.Id, dedup.Company.Id, sourceType, collectionMethod, sourceUrl, notes, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return (dedup.ExistingLead, dedup.Company, true, dedup.Message);
        }

        Company company;
        if (dedup.Company is not null)
        {
            company = dedup.Company;
            var incomingCompany = BuildCompany(userId, payload, normalizedWebsite);
            LeadDeduplicationService.MergeMissingCompanyFields(company, incomingCompany);
        }
        else
        {
            company = BuildCompany(userId, payload, normalizedWebsite);
            _db.Companies.Add(company);
        }

        var lead = BuildLead(userId, company.Id, payload, normalizedProfile);
        _db.Leads.Add(lead);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return (null, company, true, "Duplicate detected by database constraint.");
        }

        var score = _scoring.Score(company, lead);
        lead.FitScore = score.Score;
        lead.ScoreReasons = score.Reasons.ToList();
        lead.UpdatedAt = DateTimeOffset.UtcNow;
        await AddSourceAsync(userId, lead.Id, company.Id, sourceType, collectionMethod, sourceUrl, notes, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return (lead, company, false, null);
    }

    private static Company BuildCompany(Guid userId, ManualLeadPayload payload, string? normalizedWebsite)
    {
        var now = DateTimeOffset.UtcNow;
        return new Company
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = payload.CompanyName.Trim(),
            NormalizedName = UrlNormalizer.NormalizeCompanyName(payload.CompanyName),
            Website = payload.CompanyWebsite?.Trim(),
            NormalizedWebsite = normalizedWebsite,
            Industry = payload.Industry?.Trim(),
            EmployeeRange = payload.EmployeeRange?.Trim(),
            Country = payload.Country?.Trim(),
            TechnologySignals = payload.TechnologySignals?.Select(s => s.Trim()).Where(s => s.Length > 0).ToList() ?? [],
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static Lead BuildLead(Guid userId, Guid companyId, ManualLeadPayload payload, string? normalizedProfile)
    {
        var now = DateTimeOffset.UtcNow;
        return new Lead
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CompanyId = companyId,
            FullName = payload.FullName.Trim(),
            NormalizedName = UrlNormalizer.NormalizePersonName(payload.FullName),
            JobTitle = payload.JobTitle?.Trim(),
            LinkedInProfileUrl = payload.LinkedInProfileUrl?.Trim(),
            NormalizedProfileUrl = normalizedProfile,
            BusinessEmail = string.IsNullOrWhiteSpace(payload.BusinessEmail)
                ? null
                : UrlNormalizer.NormalizeEmail(payload.BusinessEmail),
            LastVerifiedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private async Task AddSourceAsync(
        Guid userId,
        Guid leadId,
        Guid companyId,
        string sourceType,
        string collectionMethod,
        string? sourceUrl,
        string? notes,
        CancellationToken cancellationToken)
    {
        _db.LeadSources.Add(new LeadSource
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LeadId = leadId,
            CompanyId = companyId,
            SourceType = sourceType,
            SourceUrl = sourceUrl,
            CollectionMethod = collectionMethod,
            CollectedAt = DateTimeOffset.UtcNow,
            PermittedUseNotes = notes,
            Evidence = notes is null ? new Dictionary<string, string>() : new Dictionary<string, string> { ["notes"] = notes }
        });
        await Task.CompletedTask;
    }

    public async Task<bool> IsSuppressedAsync(Guid userId, Lead lead, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(lead.NormalizedProfileUrl))
        {
            if (await _db.SuppressedContacts.AnyAsync(s => s.UserId == userId && s.NormalizedProfileUrl == lead.NormalizedProfileUrl, cancellationToken))
            {
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(lead.BusinessEmail))
        {
            var email = UrlNormalizer.NormalizeEmail(lead.BusinessEmail);
            if (await _db.SuppressedContacts.AnyAsync(s => s.UserId == userId && s.NormalizedEmail == email, cancellationToken))
            {
                return true;
            }
        }

        return lead.Status == LeadStatus.Suppressed;
    }
}
