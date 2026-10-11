using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CodeInspector.Leads.Infrastructure.Data;

public class LeadsDbContextFactory : IDesignTimeDbContextFactory<LeadsDbContext>
{
    public LeadsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LeadsDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=codeinspector;Username=codeinspector;Password=codeinspector;Search Path=leads,public");
        return new LeadsDbContext(optionsBuilder.Options);
    }
}
