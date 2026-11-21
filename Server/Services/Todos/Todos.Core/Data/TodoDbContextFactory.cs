using BuildingBlocks.TenantMigrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Todos.Core.Interfaces;

namespace Todos.Core.Data;

/// <summary>Builds a TodoDbContext connection for the tenant database.</summary>
public sealed class TodoDbContextFactory(IConfiguration configuration) : ITodoDbContextFactory, ITenantMigrationContextFactory
{
    public string FeatureCode => "todos";

    public TodoDbContext Create(string tenantKey)
    {
        var key = tenantKey?.Trim().ToUpperInvariant();
        if (key is not { Length: >= 2 and <= 4 } || !key.All(char.IsAsciiLetterOrDigit))
            throw new ArgumentException("A valid tenant key is required to select its database.", nameof(tenantKey));

        var baseConnectionString = configuration.GetConnectionString("PlatformDatabase");
        if (string.IsNullOrWhiteSpace(baseConnectionString))
            throw new InvalidOperationException("ConnectionStrings:PlatformDatabase is not configured.");

        var connection = new NpgsqlConnectionStringBuilder(baseConnectionString) { Database = $"tenant_{key.ToLowerInvariant()}" };
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseNpgsql(connection.ConnectionString, postgres => postgres.MigrationsHistoryTable("__TodosMigrationsHistory"))
            .Options;
        return new TodoDbContext(options);
    }

    DbContext ITenantMigrationContextFactory.Create(string tenantKey) => Create(tenantKey);
}

public sealed class TodoDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TodoDbContext>
{
    public TodoDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var tenantKey = configuration["TenantKey"];
        if (string.IsNullOrWhiteSpace(tenantKey))
            throw new InvalidOperationException("Set TenantKey when running EF Core commands for a tenant database.");
        return new TodoDbContextFactory(configuration).Create(tenantKey);
    }
}
