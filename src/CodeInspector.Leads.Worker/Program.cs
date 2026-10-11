using CodeInspector.Leads.Infrastructure;
using CodeInspector.Leads.Infrastructure.Services;
using CodeInspector.Leads.Worker.Jobs;
using Quartz;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilog((_, cfg) => cfg.ReadFrom.Configuration(builder.Configuration).WriteTo.Console());
builder.Services.AddLeadsInfrastructure(builder.Configuration);

builder.Services.AddQuartz(q =>
{
    var cron = builder.Configuration["DailyProcessing:Cron"] ?? "0 0 9 * * ?";
    var timeZoneId = builder.Configuration["DailyProcessing:TimeZone"] ?? "Asia/Kolkata";
    var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

    var jobKey = new JobKey("daily-lead-processing");
    q.AddJob<DailyLeadProcessingJob>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("daily-lead-processing-trigger")
        .WithCronSchedule(cron, x => x.InTimeZone(timeZone)));
});

builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

var host = builder.Build();
host.Run();
