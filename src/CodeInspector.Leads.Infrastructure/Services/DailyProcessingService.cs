using CodeInspector.Leads.Core.Contracts;
using CodeInspector.Leads.Core.Entities;
using CodeInspector.Leads.Core.Enums;
using CodeInspector.Leads.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeInspector.Leads.Infrastructure.Services;

public class DailyProcessingOptions
{
    public int BatchSize { get; set; } = 100;
    public int MaxAttempts { get; set; } = 5;
    public int MaxConcurrency { get; set; } = 4;
}

public record DailyProcessingSummary(
    string RunKey,
    int Selected,
    int Completed,
    int Failed,
    int Skipped,
    int DraftsCreated,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt);

public class DailyProcessingService
{
    private readonly LeadsDbContext _db;
    private readonly ILeadScoringService _scoring;
    private readonly IMessagePersonalizationService _personalization;
    private readonly LeadPersistenceService _persistence;
    private readonly ICompanyResearchBrowser _browser;
    private readonly DailyProcessingOptions _options;
    private readonly ILogger<DailyProcessingService> _logger;

    public DailyProcessingService(
        LeadsDbContext db,
        ILeadScoringService scoring,
        IMessagePersonalizationService personalization,
        LeadPersistenceService persistence,
        ICompanyResearchBrowser browser,
        IOptions<DailyProcessingOptions> options,
        ILogger<DailyProcessingService> logger)
    {
        _db = db;
        _scoring = scoring;
        _personalization = personalization;
        _persistence = persistence;
        _browser = browser;
        _options = options.Value;
        _logger = logger;
    }

    public static string BuildRunKey(DateTimeOffset when) =>
        when.ToUniversalTime().ToString("yyyy-MM-dd");

    public async Task EnsureJobsEnqueuedAsync(CancellationToken cancellationToken)
    {
        var runKey = BuildRunKey(DateTimeOffset.UtcNow);
        var companies = await _db.Companies
            .Where(c => c.Status == CompanyStatus.Candidate || c.Status == CompanyStatus.Qualified)
            .OrderBy(c => c.CreatedAt)
            .Take(_options.BatchSize * 2)
            .ToListAsync(cancellationToken);

        foreach (var company in companies)
        {
            var exists = await _db.ProcessingJobs.AnyAsync(j =>
                j.UserId == company.UserId && j.CompanyId == company.Id && j.RunKey == runKey, cancellationToken);
            if (!exists)
            {
                _db.ProcessingJobs.Add(new ProcessingJob
                {
                    Id = Guid.NewGuid(),
                    UserId = company.UserId,
                    CompanyId = company.Id,
                    RunKey = runKey,
                    Status = ProcessingJobStatus.Pending,
                    CreatedAt = DateTimeOffset.UtcNow,
                    NextAttemptAt = DateTimeOffset.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<DailyProcessingSummary> RunDailyAsync(CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        var runKey = BuildRunKey(started);
        await EnsureJobsEnqueuedAsync(cancellationToken);

        var completed = 0;
        var failed = 0;
        var skipped = 0;
        var draftsCreated = 0;
        var selected = 0;

        while (selected < _options.BatchSize)
        {
            var job = await ClaimNextJobAsync(runKey, cancellationToken);
            if (job is null)
            {
                break;
            }

            selected++;
            try
            {
                var result = await ProcessJobAsync(job, cancellationToken);
                draftsCreated += result.DraftsCreated;
                if (result.Skipped)
                {
                    skipped++;
                }
                else
                {
                    completed++;
                }
            }
            catch (Exception ex)
            {
                failed++;
                job.Attempts++;
                job.Status = job.Attempts >= _options.MaxAttempts
                    ? ProcessingJobStatus.Failed
                    : ProcessingJobStatus.Pending;
                job.LastError = ex.Message;
                job.NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(Math.Pow(2, Math.Min(job.Attempts, 6)));
                job.CompletedAt = job.Status == ProcessingJobStatus.Failed ? DateTimeOffset.UtcNow : null;
                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogError(ex, "Job {JobId} failed", job.Id);
            }
        }

        var summary = new DailyProcessingSummary(runKey, selected, completed, failed, skipped, draftsCreated, started, DateTimeOffset.UtcNow);
        _logger.LogInformation("Daily processing summary {@Summary}", summary);
        return summary;
    }

    private async Task<ProcessingJob?> ClaimNextJobAsync(string runKey, CancellationToken cancellationToken)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var job = await _db.ProcessingJobs
            .Include(j => j.Company)
            .Where(j => j.RunKey == runKey &&
                        (j.Status == ProcessingJobStatus.Pending ||
                         (j.Status == ProcessingJobStatus.Failed && j.Attempts < _options.MaxAttempts)) &&
                        (j.NextAttemptAt == null || j.NextAttemptAt <= now))
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
        {
            await tx.CommitAsync(cancellationToken);
            return null;
        }

        job.Status = ProcessingJobStatus.Running;
        job.StartedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return job;
    }

    private async Task<(bool Skipped, int DraftsCreated)> ProcessJobAsync(ProcessingJob job, CancellationToken cancellationToken)
    {
        if (job.Status == ProcessingJobStatus.Completed)
        {
            return (true, 0);
        }

        var company = job.Company ?? await _db.Companies.FirstAsync(c => c.Id == job.CompanyId, cancellationToken);
        var leads = await _db.Leads.Where(l => l.CompanyId == company.Id && l.UserId == job.UserId).ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(company.NormalizedWebsite))
        {
            var research = await _browser.ResearchCompanyWebsiteAsync(job.UserId, company.NormalizedWebsite, cancellationToken);
            if (research.NeedsManualReview)
            {
                company.Status = CompanyStatus.NeedsManualReview;
            }
            else if (research.Success)
            {
                foreach (var e in research.Evidence)
                {
                    if (!company.Evidence.Contains(e))
                    {
                        company.Evidence.Add(e);
                    }
                }

                foreach (var s in research.TechnologySignals)
                {
                    if (!company.TechnologySignals.Contains(s))
                    {
                        company.TechnologySignals.Add(s);
                    }
                }
            }
        }

        var draftsCreated = 0;
        foreach (var lead in leads)
        {
            if (await _persistence.IsSuppressedAsync(job.UserId, lead, cancellationToken))
            {
                continue;
            }

            var score = _scoring.Score(company, lead);
            lead.FitScore = score.Score;
            lead.ScoreReasons = score.Reasons.ToList();
            lead.UpdatedAt = DateTimeOffset.UtcNow;

            if (score.Score < 60)
            {
                continue;
            }

            var hasDraft = await _db.MessageDrafts.AnyAsync(d =>
                d.UserId == job.UserId &&
                d.LeadId == lead.Id &&
                d.TemplateVersion == "v1" &&
                d.Status != MessageDraftStatus.Archived, cancellationToken);

            if (hasDraft)
            {
                continue;
            }

            var message = _personalization.Generate(lead, company);
            var draft = new MessageDraft
            {
                Id = Guid.NewGuid(),
                UserId = job.UserId,
                LeadId = lead.Id,
                Channel = OutreachChannel.LinkedIn,
                TemplateVersion = message.TemplateVersion,
                Body = message.Body,
                EvidenceUsed = message.EvidenceUsed,
                Status = MessageDraftStatus.Draft,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.MessageDrafts.Add(draft);
            _db.OutreachActivities.Add(new OutreachActivity
            {
                Id = Guid.NewGuid(),
                UserId = job.UserId,
                LeadId = lead.Id,
                DraftId = draft.Id,
                ActivityType = OutreachActivityType.DraftCreated,
                OccurredAt = DateTimeOffset.UtcNow
            });
            draftsCreated++;
        }

        company.UpdatedAt = DateTimeOffset.UtcNow;
        job.Status = ProcessingJobStatus.Completed;
        job.CompletedAt = DateTimeOffset.UtcNow;
        job.LastError = null;
        await _db.SaveChangesAsync(cancellationToken);
        return (false, draftsCreated);
    }
}
