using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;

namespace BuildingBlocks.Caching;

public static class SharedCachingRegistration
{
    /// <summary>Registers shared Redis caching when configured, with an in-memory local fallback.</summary>
    public static IServiceCollection AddSharedDistributedCaches(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var redisConnection = configuration?.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnection))
            services.AddDistributedMemoryCache();
        else
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
        services.TryAddSingleton<IUserTokenCache, DistributedUserTokenCache>();
        return services;
    }
}
