using System.Text.Json;
using CodeInspector.Leads.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeInspector.Leads.Infrastructure.Data.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(500).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Website).HasMaxLength(2000);
        builder.Property(x => x.NormalizedWebsite).HasMaxLength(2000);
        builder.Property(x => x.Industry).HasMaxLength(500);
        builder.Property(x => x.EmployeeRange).HasMaxLength(100);
        builder.Property(x => x.Country).HasMaxLength(200);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.TechnologySignals)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>());
        builder.Property(x => x.Evidence)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>());
        builder.HasIndex(x => new { x.UserId, x.NormalizedWebsite }).IsUnique()
            .HasFilter("\"NormalizedWebsite\" IS NOT NULL");
        builder.HasIndex(x => new { x.UserId, x.NormalizedName, x.Country });
    }
}
