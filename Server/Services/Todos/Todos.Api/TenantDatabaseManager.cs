using System.Collections.Concurrent;
using BuildingBlocks.UserContext;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Todos.Api;

public sealed class TenantDatabaseConnectionFactory
{
    private readonly string _adminConnectionString;

    public TenantDatabaseConnectionFactory(IConfiguration configuration)
    {
        _adminConnectionString = configuration.GetConnectionString("TenantDatabaseAdmin")
            ?? configuration.GetConnectionString("PlatformDatabase")
            ?? throw new InvalidOperationException(
                "Configure ConnectionStrings:TenantDatabaseAdmin for tenant database provisioning.");
    }

    public string GetDatabaseName(string tenantKey)
    {
        if (tenantKey.Length is < 2 or > 4 || !tenantKey.All(char.IsAsciiLetterOrDigit))
            throw new InvalidOperationException("The authenticated tenant key is invalid for database routing.");

        return $"erp_tenant_{tenantKey.ToLowerInvariant()}";
    }

    public string GetTenantConnectionString(string tenantKey)
    {
        var builder = new NpgsqlConnectionStringBuilder(_adminConnectionString)
        {
            Database = GetDatabaseName(tenantKey)
        };
        return builder.ConnectionString;
    }

    public string GetAdminConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder(_adminConnectionString)
        {
            Database = "postgres"
        };
        return builder.ConnectionString;
    }
}

public sealed class TenantDatabaseManager(TenantDatabaseConnectionFactory connectionFactory)
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _initialized = new(StringComparer.Ordinal);

    public async Task EnsureReadyAsync(string tenantKey, CancellationToken cancellationToken)
    {
        var databaseName = connectionFactory.GetDatabaseName(tenantKey);
        if (_initialized.ContainsKey(databaseName)) return;

        var gate = _locks.GetOrAdd(databaseName, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (_initialized.ContainsKey(databaseName)) return;

            await EnsureDatabaseExistsAsync(databaseName, cancellationToken);

            var options = new DbContextOptionsBuilder<TodoDbContext>()
                .UseNpgsql(connectionFactory.GetTenantConnectionString(tenantKey))
                .Options;
            await using var db = new TodoDbContext(options);
            await db.Database.MigrateAsync(cancellationToken);
            _initialized.TryAdd(databaseName, 0);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task EnsureDatabaseExistsAsync(string databaseName, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionFactory.GetAdminConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using (var exists = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = @name)", connection))
        {
            exists.Parameters.AddWithValue("name", databaseName);
            if ((bool)(await exists.ExecuteScalarAsync(cancellationToken) ?? false)) return;
        }

        // The database name is generated only from the validated, signed tenant key.
        await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", connection);
        try
        {
            await create.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.DuplicateDatabase)
        {
            // Another API replica created this tenant database after our existence check.
        }
    }
}

public sealed class TenantDatabaseMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IUserContextAccessor accessor, TenantDatabaseManager databases)
    {
        if (context.Request.Path.StartsWithSegments("/api/v1/todos") && accessor.Current is { } user)
            await databases.EnsureReadyAsync(user.TenantKey, context.RequestAborted);

        await next(context);
    }
}
