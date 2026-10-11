using CodeInspector.Leads.Api.Auth;
using CodeInspector.Leads.Api.Dtos;
using CodeInspector.Leads.Core.Contracts;
using CodeInspector.Leads.Core.Entities;
using CodeInspector.Leads.Core.Enums;
using CodeInspector.Leads.Infrastructure.Data;
using CodeInspector.Leads.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CodeInspector.Leads.Api.Controllers;

[ApiController]
[Authorize]
public class DraftsController(
    LeadsDbContext db,
    IUserContext user,
    IMessagePersonalizationService personalization,
    LeadPersistenceService persistence) : ControllerBase
{
    [HttpPost("api/v1/leads/{leadId:guid}/drafts")]
    public async Task<ActionResult<DraftResponse>> Create(Guid leadId, CreateDraftRequest request, CancellationToken cancellationToken)
    {
        var lead = await db.Leads.Include(l => l.Company)
            .FirstOrDefaultAsync(l => l.Id == leadId && l.UserId == user.UserId, cancellationToken);
        if (lead is null)
        {
            return NotFound();
        }

        if (await persistence.IsSuppressedAsync(user.UserId, lead, cancellationToken))
        {
            return BadRequest(new { error = "Lead is suppressed." });
        }

        var message = personalization.Generate(lead, lead.Company);
        var draft = new MessageDraft
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId,
            LeadId = leadId,
            Channel = request.Channel,
            TemplateVersion = message.TemplateVersion,
            Body = message.Body,
            EvidenceUsed = message.EvidenceUsed,
            Status = MessageDraftStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.MessageDrafts.Add(draft);
        db.OutreachActivities.Add(new OutreachActivity
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId,
            LeadId = leadId,
            DraftId = draft.Id,
            ActivityType = OutreachActivityType.DraftCreated,
            OccurredAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(Map(draft));
    }

    [HttpPatch("api/v1/drafts/{id:guid}")]
    public async Task<ActionResult<DraftResponse>> Patch(Guid id, PatchDraftRequest request, CancellationToken cancellationToken)
    {
        var draft = await db.MessageDrafts.FirstOrDefaultAsync(d => d.Id == id && d.UserId == user.UserId, cancellationToken);
        if (draft is null)
        {
            return NotFound();
        }

        draft.Body = request.Body;
        draft.Subject = request.Subject;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(Map(draft));
    }

    [HttpPost("api/v1/drafts/{id:guid}/approve")]
    public async Task<ActionResult<DraftResponse>> Approve(Guid id, CancellationToken cancellationToken)
    {
        var draft = await db.MessageDrafts.FirstOrDefaultAsync(d => d.Id == id && d.UserId == user.UserId, cancellationToken);
        if (draft is null)
        {
            return NotFound();
        }

        draft.Status = MessageDraftStatus.Approved;
        draft.ReviewedAt = DateTimeOffset.UtcNow;
        db.OutreachActivities.Add(new OutreachActivity
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId,
            LeadId = draft.LeadId,
            DraftId = draft.Id,
            ActivityType = OutreachActivityType.DraftApproved,
            OccurredAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(Map(draft));
    }

    [HttpPost("api/v1/drafts/{id:guid}/archive")]
    public async Task<ActionResult<DraftResponse>> Archive(Guid id, CancellationToken cancellationToken)
    {
        var draft = await db.MessageDrafts.FirstOrDefaultAsync(d => d.Id == id && d.UserId == user.UserId, cancellationToken);
        if (draft is null)
        {
            return NotFound();
        }

        draft.Status = MessageDraftStatus.Archived;
        draft.ReviewedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(Map(draft));
    }

    private static DraftResponse Map(MessageDraft d) =>
        new(d.Id, d.LeadId, d.Channel.ToString(), d.TemplateVersion, d.Subject, d.Body, d.EvidenceUsed, d.Status.ToString(), d.CreatedAt);
}
