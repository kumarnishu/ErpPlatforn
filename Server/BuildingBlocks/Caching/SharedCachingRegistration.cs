using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Caching;

public static class SharedCachingRegistration
{
    /// <summary>Registers the in-memory distributed cache for local development.</summary>
    public static IServiceCollection AddSharedDistributedCaches(this IServiceCollection services)
    {
        services.AddDistributedMemoryCache();
        services.TryAddSingleton<IUserTokenCache, DistributedUserTokenCache>();
        return services;
    }
}
