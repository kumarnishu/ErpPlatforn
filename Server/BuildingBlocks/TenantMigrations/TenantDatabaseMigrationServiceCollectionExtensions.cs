using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.TenantMigrations;

/// <summary>Registers the shared tenant database migration coordinator.</summary>
public static class TenantDatabaseMigrationServiceCollectionExtensions
{
    public static IServiceCollection AddTenantDatabaseMigrations(this IServiceCollection services)
    {
        services.TryAddScoped<ITenantDatabaseMigrationService, TenantDatabaseMigrationService>();
        return services;
    }
}
