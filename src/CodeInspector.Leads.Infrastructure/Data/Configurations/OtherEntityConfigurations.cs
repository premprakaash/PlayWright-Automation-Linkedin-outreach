using System.Text.Json;
using CodeInspector.Leads.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeInspector.Leads.Infrastructure.Data.Configurations;

public class ResearchSessionConfiguration : IEntityTypeConfiguration<ResearchSession>
{
    public void Configure(EntityTypeBuilder<ResearchSession> builder)
    {
        builder.ToTable("research_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Keywords).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SearchUrl).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.HasIndex(x => new { x.UserId, x.OpenedAt });
    }
}

public class LeadSourceConfiguration : IEntityTypeConfiguration<LeadSource>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<LeadSource> builder)
    {
        builder.ToTable("lead_sources");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SourceType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SourceUrl).HasMaxLength(2000);
        builder.Property(x => x.CollectionMethod).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Evidence)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonOptions) ?? new Dictionary<string, string>());
    }
}

public class ProcessingJobConfiguration : IEntityTypeConfiguration<ProcessingJob>
{
    public void Configure(EntityTypeBuilder<ProcessingJob> builder)
    {
        builder.ToTable("processing_jobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RunKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.LastError).HasMaxLength(4000);
        builder.HasOne(x => x.Company).WithMany(x => x.ProcessingJobs).HasForeignKey(x => x.CompanyId);
        builder.HasIndex(x => new { x.UserId, x.CompanyId, x.RunKey }).IsUnique();
        builder.HasIndex(x => new { x.Status, x.NextAttemptAt });
    }
}

public class MessageDraftConfiguration : IEntityTypeConfiguration<MessageDraft>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<MessageDraft> builder)
    {
        builder.ToTable("message_drafts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TemplateVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(500);
        builder.Property(x => x.Body).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.EvidenceUsed)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonOptions) ?? new Dictionary<string, string>());
        builder.HasOne(x => x.Lead).WithMany(x => x.Drafts).HasForeignKey(x => x.LeadId);
        builder.HasIndex(x => new { x.UserId, x.LeadId, x.TemplateVersion, x.Status });
    }
}

public class OutreachActivityConfiguration : IEntityTypeConfiguration<OutreachActivity>
{
    public void Configure(EntityTypeBuilder<OutreachActivity> builder)
    {
        builder.ToTable("outreach_activities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActivityType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.HasOne(x => x.Lead).WithMany(x => x.Activities).HasForeignKey(x => x.LeadId);
        builder.HasIndex(x => new { x.UserId, x.LeadId, x.OccurredAt });
    }
}

public class SuppressedContactConfiguration : IEntityTypeConfiguration<SuppressedContact>
{
    public void Configure(EntityTypeBuilder<SuppressedContact> builder)
    {
        builder.ToTable("suppressed_contacts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NormalizedProfileUrl).HasMaxLength(2000);
        builder.Property(x => x.NormalizedEmail).HasMaxLength(500);
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.NormalizedProfileUrl }).IsUnique()
            .HasFilter("\"NormalizedProfileUrl\" IS NOT NULL");
        builder.HasIndex(x => new { x.UserId, x.NormalizedEmail }).IsUnique()
            .HasFilter("\"NormalizedEmail\" IS NOT NULL");
    }
}

public class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<ImportBatch> builder)
    {
        builder.ToTable("import_batches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(500);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.RowErrors)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<ImportRowError>>(v, JsonOptions) ?? new List<ImportRowError>());
    }
}
