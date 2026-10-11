using CodeInspector.Leads.Core.Contracts;
using CodeInspector.Leads.Core.Services;
using CodeInspector.Leads.Infrastructure.Browser;
using CodeInspector.Leads.Infrastructure.Connectors;
using CodeInspector.Leads.Infrastructure.Data;
using CodeInspector.Leads.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeInspector.Leads.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLeadsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CsvImportOptions>(configuration.GetSection("Import"));
        services.Configure<DailyProcessingOptions>(configuration.GetSection("DailyProcessing"));
        services.Configure<BrowserOptions>(configuration.GetSection("Browser"));

        services.AddDbContext<LeadsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("LeadsDatabase")));

        services.AddScoped<LeadDeduplicationService>();
        services.AddScoped<LeadPersistenceService>();
        services.AddScoped<DailyProcessingService>();
        services.AddSingleton<ILeadScoringService, LeadScoringService>();
        services.AddSingleton<IMessagePersonalizationService, TemplateMessagePersonalizationService>();
        services.AddSingleton<ICompanyResearchBrowser, PlaywrightCompanyResearchBrowser>();

        services.AddScoped<ILeadSourceConnector, ManualLeadConnector>();
        services.AddScoped<ILeadSourceConnector, CsvLeadConnector>();
        services.AddScoped<ManualLeadConnector>();
        services.AddScoped<CsvLeadConnector>();

        return services;
    }
}
