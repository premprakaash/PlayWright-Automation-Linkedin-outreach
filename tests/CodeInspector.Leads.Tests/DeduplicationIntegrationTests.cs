using CodeInspector.Leads.Core.Contracts;
using CodeInspector.Leads.Core.Services;
using CodeInspector.Leads.Infrastructure.Connectors;
using CodeInspector.Leads.Infrastructure.Data;
using CodeInspector.Leads.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CodeInspector.Leads.Tests;

public class DeduplicationIntegrationTests
{
    private static LeadsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LeadsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LeadsDbContext(options);
    }

    [Fact]
    public async Task DuplicateProfileUrl_IsDetectedOnSecondImport()
    {
        await using var db = CreateDb();
        var dedup = new LeadDeduplicationService(db);
        var persistence = new LeadPersistenceService(db, dedup, new LeadScoringService());
        var manual = new ManualLeadConnector(persistence, db);
        var userId = Guid.NewGuid();

        var payload = new ManualLeadPayload(
            "Acme",
            "https://acme.example",
            "US",
            "SaaS",
            "11-50",
            [".NET"],
            "Jane Doe",
            "CTO",
            "https://www.linkedin.com/in/jane-doe",
            "jane@acme.example",
            null);

        var first = await manual.ImportAsync(new LeadImportInput(userId, "manual", "test", null, null, null, payload), CancellationToken.None);
        Assert.Equal(1, first.ImportedRows);

        var second = await manual.ImportAsync(new LeadImportInput(userId, "manual", "test", null, null, null, payload with { BusinessEmail = "jane2@acme.example" }), CancellationToken.None);
        Assert.Equal(1, second.DuplicateRows);
        Assert.Equal(1, await db.Leads.CountAsync());
    }

    [Fact]
    public async Task TenantIsolation_DoesNotMatchOtherUserDuplicates()
    {
        await using var db = CreateDb();
        var dedup = new LeadDeduplicationService(db);
        var persistence = new LeadPersistenceService(db, dedup, new LeadScoringService());
        var manual = new ManualLeadConnector(persistence, db);

        var payload = new ManualLeadPayload(
            "Acme", null, null, "SaaS", null, null,
            "Jane Doe", "CTO", "https://www.linkedin.com/in/jane-doe", null, null);

        await manual.ImportAsync(new LeadImportInput(Guid.NewGuid(), "manual", "test", null, null, null, payload), CancellationToken.None);
        var secondUser = await manual.ImportAsync(new LeadImportInput(Guid.NewGuid(), "manual", "test", null, null, null, payload), CancellationToken.None);
        Assert.Equal(1, secondUser.ImportedRows);
        Assert.Equal(2, await db.Leads.CountAsync());
    }
}
