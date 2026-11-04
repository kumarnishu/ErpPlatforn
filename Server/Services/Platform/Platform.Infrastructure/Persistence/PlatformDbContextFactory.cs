using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Platform.Infrastructure.Persistence;

/// <summary>Provides EF tooling a SQL Server model without connecting to a database.</summary>
public sealed class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=PlatformDesignTime;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new PlatformDbContext(options);
    }
}
