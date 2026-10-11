using CodeInspector.Leads.Api.Auth;
using CodeInspector.Leads.Api.Dtos;
using CodeInspector.Leads.Core.Entities;
using CodeInspector.Leads.Core.Enums;
using CodeInspector.Leads.Core.Services;
using CodeInspector.Leads.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CodeInspector.Leads.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/research-sessions")]
public class ResearchSessionsController(LeadsDbContext db, IUserContext user) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ResearchSessionResponse>> Create(CreateResearchSessionRequest request, CancellationToken cancellationToken)
    {
        var searchUrl = LinkedInSearchUrlBuilder.BuildPeopleSearchUrl(request.Keywords);
        var session = new ResearchSession
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId,
            Keywords = request.Keywords.Trim(),
            SearchUrl = searchUrl,
            Status = ResearchSessionStatus.Created,
            OpenedAt = DateTimeOffset.UtcNow
        };
        db.ResearchSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = session.Id }, Map(session));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ResearchSessionResponse>>> List(CancellationToken cancellationToken)
    {
        var items = await db.ResearchSessions
            .Where(x => x.UserId == user.UserId)
            .OrderByDescending(x => x.OpenedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        return Ok(items.Select(Map));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ResearchSessionResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var session = await db.ResearchSessions.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.UserId, cancellationToken);
        return session is null ? NotFound() : Ok(Map(session));
    }

    private static ResearchSessionResponse Map(ResearchSession s) =>
        new(s.Id, s.Keywords, s.SearchUrl, s.Status.ToString(), s.OpenedAt, s.CompletedAt, s.Notes);
}
