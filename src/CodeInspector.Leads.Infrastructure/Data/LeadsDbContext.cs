using CodeInspector.Leads.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeInspector.Leads.Infrastructure.Data;

public class LeadsDbContext : DbContext
{
    public LeadsDbContext(DbContextOptions<LeadsDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<ResearchSession> ResearchSessions => Set<ResearchSession>();
    public DbSet<LeadSource> LeadSources => Set<LeadSource>();
    public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();
    public DbSet<MessageDraft> MessageDrafts => Set<MessageDraft>();
    public DbSet<OutreachActivity> OutreachActivities => Set<OutreachActivity>();
    public DbSet<SuppressedContact> SuppressedContacts => Set<SuppressedContact>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("leads");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LeadsDbContext).Assembly);
    }
}
