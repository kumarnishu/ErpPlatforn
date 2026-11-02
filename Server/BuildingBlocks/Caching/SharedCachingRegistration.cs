using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Caching;

public static class SharedCachingRegistration
{
    /// <summary>Register after configuring the host's IDistributedCache provider (for example, Redis).</summary>
    public static IServiceCollection AddSharedDistributedCaches(this IServiceCollection services)
    {
        services.TryAddSingleton<IUserContextCache, DistributedUserContextCache>();
        services.TryAddSingleton<ITenantDatabaseRouteCache, DistributedTenantDatabaseRouteCache>();
        return services;
    }
}
