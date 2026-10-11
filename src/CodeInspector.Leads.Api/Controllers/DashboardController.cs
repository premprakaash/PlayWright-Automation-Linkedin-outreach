using CodeInspector.Leads.Api.Auth;
using CodeInspector.Leads.Api.Dtos;
using CodeInspector.Leads.Core.Enums;
using CodeInspector.Leads.Infrastructure.Data;
using CodeInspector.Leads.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CodeInspector.Leads.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/dashboard")]
public class DashboardController(LeadsDbContext db, IUserContext user) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> Summary(CancellationToken cancellationToken)
    {
        var uid = user.UserId;
        var totalCompanies = await db.Companies.CountAsync(c => c.UserId == uid, cancellationToken);
        var totalLeads = await db.Leads.CountAsync(l => l.UserId == uid, cancellationToken);
        var qualifiedLeads = await db.Leads.CountAsync(l => l.UserId == uid && l.FitScore >= 60, cancellationToken);
        var draftsAwaiting = await db.MessageDrafts.CountAsync(d => d.UserId == uid && d.Status == MessageDraftStatus.Draft, cancellationToken);
        var sent = await db.OutreachActivities.CountAsync(a => a.UserId == uid && a.ActivityType == OutreachActivityType.SentManually, cancellationToken);
        var replies = await db.OutreachActivities.CountAsync(a => a.UserId == uid && a.ActivityType == OutreachActivityType.ReplyReceived, cancellationToken);
        var positive = await db.OutreachActivities.CountAsync(a => a.UserId == uid && a.ActivityType == OutreachActivityType.PositiveReply, cancellationToken);
        var demos = await db.OutreachActivities.CountAsync(a => a.UserId == uid && a.ActivityType == OutreachActivityType.DemoBooked, cancellationToken);
        var conversions = await db.OutreachActivities.CountAsync(a => a.UserId == uid && a.ActivityType == OutreachActivityType.Converted, cancellationToken);
        var failedJobs = await db.ProcessingJobs.CountAsync(j => j.UserId == uid && j.Status == ProcessingJobStatus.Failed, cancellationToken);

        var runKey = DailyProcessingService.BuildRunKey(DateTimeOffset.UtcNow);
        var latestRun = await db.ProcessingJobs
            .Where(j => j.UserId == uid && j.RunKey == runKey)
            .GroupBy(j => j.RunKey)
            .Select(g => new
            {
                RunKey = g.Key,
                Total = g.Count(),
                Completed = g.Count(x => x.Status == ProcessingJobStatus.Completed),
                Failed = g.Count(x => x.Status == ProcessingJobStatus.Failed)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new DashboardSummaryResponse(
            totalCompanies,
            totalLeads,
            qualifiedLeads,
            draftsAwaiting,
            sent,
            replies,
            positive,
            demos,
            conversions,
            latestRun,
            failedJobs));
    }

    [HttpGet("daily-processing")]
    public async Task<IActionResult> DailyProcessing(CancellationToken cancellationToken)
    {
        var uid = user.UserId;
        var jobs = await db.ProcessingJobs
            .Where(j => j.UserId == uid)
            .OrderByDescending(j => j.CreatedAt)
            .Take(200)
            .Select(j => new
            {
                j.Id,
                j.CompanyId,
                j.RunKey,
                Status = j.Status.ToString(),
                j.Attempts,
                j.LastError,
                j.StartedAt,
                j.CompletedAt
            })
            .ToListAsync(cancellationToken);
        return Ok(jobs);
    }
}
