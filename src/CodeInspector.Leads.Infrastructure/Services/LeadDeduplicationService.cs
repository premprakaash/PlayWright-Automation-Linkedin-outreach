using CodeInspector.Leads.Core.Entities;
using CodeInspector.Leads.Core.Normalization;
using CodeInspector.Leads.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CodeInspector.Leads.Infrastructure.Services;

public enum DeduplicationOutcome
{
    New,
    DuplicateLead,
    AmbiguousCompany
}

public record DeduplicationResult(DeduplicationOutcome Outcome, Company? Company, Lead? ExistingLead, string? Message);

public class LeadDeduplicationService
{
    private readonly LeadsDbContext _db;

    public LeadDeduplicationService(LeadsDbContext db)
    {
        _db = db;
    }

    public async Task<DeduplicationResult> ResolveAsync(
        Guid userId,
        string companyName,
        string? website,
        string? country,
        string fullName,
        string? linkedInUrl,
        string? businessEmail,
        CancellationToken cancellationToken)
    {
        string? normalizedProfile = null;
        if (!string.IsNullOrWhiteSpace(linkedInUrl))
        {
            if (!UrlNormalizer.TryNormalizeLinkedInProfileUrl(linkedInUrl, out normalizedProfile, out _))
            {
                return new DeduplicationResult(DeduplicationOutcome.AmbiguousCompany, null, null, "Invalid LinkedIn profile URL.");
            }

            var byProfile = await _db.Leads.AsNoTracking()
                .FirstOrDefaultAsync(l => l.UserId == userId && l.NormalizedProfileUrl == normalizedProfile, cancellationToken);
            if (byProfile is not null)
            {
                var company = await _db.Companies.FirstAsync(c => c.Id == byProfile.CompanyId, cancellationToken);
                return new DeduplicationResult(DeduplicationOutcome.DuplicateLead, company, byProfile, "Duplicate normalized profile URL.");
            }
        }

        if (!string.IsNullOrWhiteSpace(businessEmail))
        {
            var normalizedEmail = UrlNormalizer.NormalizeEmail(businessEmail);
            var byEmail = await _db.Leads.AsNoTracking()
                .FirstOrDefaultAsync(l => l.UserId == userId && l.BusinessEmail == normalizedEmail, cancellationToken);
            if (byEmail is not null)
            {
                var company = await _db.Companies.FirstAsync(c => c.Id == byEmail.CompanyId, cancellationToken);
                return new DeduplicationResult(DeduplicationOutcome.DuplicateLead, company, byEmail, "Duplicate business email.");
            }
        }

        string? normalizedWebsite = null;
        if (!string.IsNullOrWhiteSpace(website) && UrlNormalizer.TryNormalizeWebsite(website, out var nw, out _))
        {
            normalizedWebsite = nw;
            var byWebsite = await _db.Companies
                .FirstOrDefaultAsync(c => c.UserId == userId && c.NormalizedWebsite == normalizedWebsite, cancellationToken);
            if (byWebsite is not null)
            {
                return new DeduplicationResult(DeduplicationOutcome.New, byWebsite, null, null);
            }
        }

        var normalizedName = UrlNormalizer.NormalizeCompanyName(companyName);
        var nameMatches = await _db.Companies
            .Where(c => c.UserId == userId && c.NormalizedName == normalizedName)
            .ToListAsync(cancellationToken);

        if (nameMatches.Count == 1)
        {
            return new DeduplicationResult(DeduplicationOutcome.New, nameMatches[0], null, null);
        }

        if (nameMatches.Count > 1)
        {
            if (!string.IsNullOrWhiteSpace(country))
            {
                var countryMatch = nameMatches.FirstOrDefault(c =>
                    string.Equals(c.Country, country, StringComparison.OrdinalIgnoreCase));
                if (countryMatch is not null)
                {
                    return new DeduplicationResult(DeduplicationOutcome.New, countryMatch, null, null);
                }
            }

            return new DeduplicationResult(DeduplicationOutcome.AmbiguousCompany, null, null,
                "Multiple companies match name; manual review required.");
        }

        return new DeduplicationResult(DeduplicationOutcome.New, null, null, null);
    }

    public static void MergeMissingFields(Lead existing, Lead incoming)
    {
        if (string.IsNullOrWhiteSpace(existing.JobTitle) && !string.IsNullOrWhiteSpace(incoming.JobTitle))
        {
            existing.JobTitle = incoming.JobTitle;
        }

        if (string.IsNullOrWhiteSpace(existing.BusinessEmail) && !string.IsNullOrWhiteSpace(incoming.BusinessEmail))
        {
            existing.BusinessEmail = incoming.BusinessEmail;
        }

        if (string.IsNullOrWhiteSpace(existing.LinkedInProfileUrl) && !string.IsNullOrWhiteSpace(incoming.LinkedInProfileUrl))
        {
            existing.LinkedInProfileUrl = incoming.LinkedInProfileUrl;
            existing.NormalizedProfileUrl = incoming.NormalizedProfileUrl;
        }

        if (!existing.LastVerifiedAt.HasValue && incoming.LastVerifiedAt.HasValue)
        {
            existing.LastVerifiedAt = incoming.LastVerifiedAt;
        }

        existing.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static void MergeMissingCompanyFields(Company existing, Company incoming)
    {
        if (string.IsNullOrWhiteSpace(existing.Website) && !string.IsNullOrWhiteSpace(incoming.Website))
        {
            existing.Website = incoming.Website;
            existing.NormalizedWebsite = incoming.NormalizedWebsite;
        }

        if (string.IsNullOrWhiteSpace(existing.Industry) && !string.IsNullOrWhiteSpace(incoming.Industry))
        {
            existing.Industry = incoming.Industry;
        }

        if (string.IsNullOrWhiteSpace(existing.EmployeeRange) && !string.IsNullOrWhiteSpace(incoming.EmployeeRange))
        {
            existing.EmployeeRange = incoming.EmployeeRange;
        }

        if (string.IsNullOrWhiteSpace(existing.Country) && !string.IsNullOrWhiteSpace(incoming.Country))
        {
            existing.Country = incoming.Country;
        }

        foreach (var signal in incoming.TechnologySignals)
        {
            if (!existing.TechnologySignals.Contains(signal, StringComparer.OrdinalIgnoreCase))
            {
                existing.TechnologySignals.Add(signal);
            }
        }

        existing.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
