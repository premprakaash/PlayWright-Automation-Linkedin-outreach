using CodeInspector.Leads.Api.Auth;
using CodeInspector.Leads.Infrastructure.Data;
using CodeInspector.Leads.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CodeInspector.Leads.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/jobs")]
public class JobsController(LeadsDbContext db, IUserContext user, DailyProcessingService processing) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var jobs = await db.ProcessingJobs
            .Where(j => j.UserId == user.UserId)
            .OrderByDescending(j => j.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        return Ok(jobs);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var job = await db.ProcessingJobs.FirstOrDefaultAsync(j => j.Id == id && j.UserId == user.UserId, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpPost("run-daily")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("manual-job")]
    public async Task<IActionResult> RunDaily(CancellationToken cancellationToken)
    {
        if (!user.IsAdmin)
        {
            return Forbid();
        }

        var summary = await processing.RunDailyAsync(cancellationToken);
        return Accepted(summary);
    }
}
