using CodeInspector.Leads.Infrastructure.Services;
using Quartz;

namespace CodeInspector.Leads.Worker.Jobs;

public class DailyLeadProcessingJob(DailyProcessingService processing, ILogger<DailyLeadProcessingJob> logger) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting daily lead processing job");
        var summary = await processing.RunDailyAsync(cancellationToken != default ? cancellationToken : context.CancellationToken);
        logger.LogInformation("Daily lead processing completed {@Summary}", summary);
    }
}
