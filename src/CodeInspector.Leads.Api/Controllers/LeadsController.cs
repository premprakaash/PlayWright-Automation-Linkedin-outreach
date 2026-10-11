using CodeInspector.Leads.Api.Auth;
using CodeInspector.Leads.Api.Dtos;
using CodeInspector.Leads.Core.Contracts;
using CodeInspector.Leads.Core.Entities;
using CodeInspector.Leads.Core.Enums;
using CodeInspector.Leads.Core.Normalization;
using CodeInspector.Leads.Infrastructure.Connectors;
using CodeInspector.Leads.Infrastructure.Data;
using CodeInspector.Leads.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CodeInspector.Leads.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/leads")]
public class LeadsController(
    LeadsDbContext db,
    IUserContext user,
    ManualLeadConnector manualConnector,
    ILeadScoringService scoring) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<LeadResponse>> Create(CreateLeadRequest request, CancellationToken cancellationToken)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == request.CompanyId && c.UserId == user.UserId, cancellationToken);
        if (company is null)
        {
            return BadRequest(new { error = "Company not found." });
        }

        var import = await manualConnector.ImportAsync(new LeadImportInput(
            user.UserId,
            "manual",
            "api",
            null,
            request.Notes,
            request.Notes is null ? null : new Dictionary<string, string> { ["notes"] = request.Notes },
            new ManualLeadPayload(
                company.Name,
                company.Website,
                company.Country,
                company.Industry,
                company.EmployeeRange,
                company.TechnologySignals,
                request.FullName,
                request.JobTitle,
                request.LinkedInProfileUrl,
                request.BusinessEmail,
                request.Notes)), cancellationToken);

        var row = import.RowResults.FirstOrDefault();
        if (row?.Error is not null)
        {
            return BadRequest(new { error = row.Error });
        }

        if (row?.LeadId is null)
        {
            return BadRequest(new { error = "Unable to create lead." });
        }

        if (request.ResearchSessionId.HasValue)
        {
            var lead = await db.Leads.FirstAsync(l => l.Id == row.LeadId && l.UserId == user.UserId, cancellationToken);
            lead.ResearchSessionId = request.ResearchSessionId;
            await db.SaveChangesAsync(cancellationToken);
        }

        return await GetById(row.LeadId.Value, cancellationToken);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LeadResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] int? minScore,
        CancellationToken cancellationToken)
    {
        var q = db.Leads.Include(l => l.Company).Where(l => l.UserId == user.UserId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            q = q.Where(l => l.NormalizedName.Contains(s) || (l.JobTitle != null && l.JobTitle.ToLower().Contains(s)));
        }

        if (minScore.HasValue)
        {
            q = q.Where(l => l.FitScore >= minScore.Value);
        }

        var items = await q.OrderByDescending(l => l.FitScore).Take(200).ToListAsync(cancellationToken);
        return Ok(items.Select(Map));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeadResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var lead = await db.Leads.Include(l => l.Company)
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == user.UserId, cancellationToken);
        return lead is null ? NotFound() : Ok(Map(lead));
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<LeadResponse>> Patch(Guid id, PatchLeadRequest request, CancellationToken cancellationToken)
    {
        var lead = await db.Leads.Include(l => l.Company)
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == user.UserId, cancellationToken);
        if (lead is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            lead.FullName = request.FullName.Trim();
            lead.NormalizedName = UrlNormalizer.NormalizePersonName(request.FullName);
        }

        if (request.JobTitle is not null)
        {
            lead.JobTitle = request.JobTitle;
        }

        if (request.LinkedInProfileUrl is not null)
        {
            if (string.IsNullOrWhiteSpace(request.LinkedInProfileUrl))
            {
                lead.LinkedInProfileUrl = null;
                lead.NormalizedProfileUrl = null;
            }
            else if (UrlNormalizer.TryNormalizeLinkedInProfileUrl(request.LinkedInProfileUrl, out var normalized, out var err))
            {
                lead.LinkedInProfileUrl = request.LinkedInProfileUrl.Trim();
                lead.NormalizedProfileUrl = normalized;
            }
            else
            {
                return BadRequest(new { error = err });
            }
        }

        if (request.BusinessEmail is not null)
        {
            lead.BusinessEmail = string.IsNullOrWhiteSpace(request.BusinessEmail)
                ? null
                : UrlNormalizer.NormalizeEmail(request.BusinessEmail);
        }

        if (request.Status.HasValue)
        {
            lead.Status = request.Status.Value;
        }

        lead.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(Map(lead));
    }

    [HttpPost("{id:guid}/score")]
    public async Task<ActionResult<LeadResponse>> Score(Guid id, CancellationToken cancellationToken)
    {
        var lead = await db.Leads.Include(l => l.Company)
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == user.UserId, cancellationToken);
        if (lead is null)
        {
            return NotFound();
        }

        var result = scoring.Score(lead.Company, lead);
        lead.FitScore = result.Score;
        lead.ScoreReasons = result.Reasons.ToList();
        lead.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(Map(lead));
    }

    [HttpPost("{id:guid}/suppress")]
    public async Task<IActionResult> Suppress(Guid id, SuppressLeadRequest request, CancellationToken cancellationToken)
    {
        var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id && l.UserId == user.UserId, cancellationToken);
        if (lead is null)
        {
            return NotFound();
        }

        db.SuppressedContacts.Add(new SuppressedContact
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId,
            NormalizedProfileUrl = lead.NormalizedProfileUrl,
            NormalizedEmail = lead.BusinessEmail is null ? null : UrlNormalizer.NormalizeEmail(lead.BusinessEmail),
            Reason = request.Reason,
            CreatedAt = DateTimeOffset.UtcNow
        });
        lead.Status = LeadStatus.Suppressed;
        lead.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/activities")]
    public async Task<IActionResult> AddActivity(Guid id, CreateActivityRequest request, CancellationToken cancellationToken)
    {
        var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id && l.UserId == user.UserId, cancellationToken);
        if (lead is null)
        {
            return NotFound();
        }

        db.OutreachActivities.Add(new OutreachActivity
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId,
            LeadId = id,
            DraftId = request.DraftId,
            ActivityType = request.ActivityType,
            Notes = request.Notes,
            OccurredAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/activities")]
    public async Task<IActionResult> GetActivities(Guid id, CancellationToken cancellationToken)
    {
        var exists = await db.Leads.AnyAsync(l => l.Id == id && l.UserId == user.UserId, cancellationToken);
        if (!exists)
        {
            return NotFound();
        }

        var activities = await db.OutreachActivities
            .Where(a => a.LeadId == id && a.UserId == user.UserId)
            .OrderByDescending(a => a.OccurredAt)
            .ToListAsync(cancellationToken);
        return Ok(activities);
    }

    private static LeadResponse Map(Lead l) =>
        new(l.Id, l.CompanyId, l.FullName, l.JobTitle, l.LinkedInProfileUrl, l.BusinessEmail, l.FitScore, l.ScoreReasons,
            l.Status.ToString(), l.CreatedAt, l.UpdatedAt,
            l.Company is null ? null : CompaniesController.Map(l.Company));
}
