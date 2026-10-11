using System.Text.Json;
using CodeInspector.Leads.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeInspector.Leads.Infrastructure.Data.Configurations;

public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("leads");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FullName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.JobTitle).HasMaxLength(500);
        builder.Property(x => x.LinkedInProfileUrl).HasMaxLength(2000);
        builder.Property(x => x.NormalizedProfileUrl).HasMaxLength(2000);
        builder.Property(x => x.BusinessEmail).HasMaxLength(500);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ScoreReasons)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<ScoreReasonEntry>>(v, JsonOptions) ?? new List<ScoreReasonEntry>());
        builder.HasOne(x => x.Company).WithMany(x => x.Leads).HasForeignKey(x => x.CompanyId);
        builder.HasOne(x => x.ResearchSession).WithMany(x => x.Leads).HasForeignKey(x => x.ResearchSessionId);
        builder.HasIndex(x => new { x.UserId, x.NormalizedProfileUrl }).IsUnique()
            .HasFilter("\"NormalizedProfileUrl\" IS NOT NULL");
        builder.HasIndex(x => new { x.UserId, x.BusinessEmail }).IsUnique()
            .HasFilter("\"BusinessEmail\" IS NOT NULL");
    }
}
