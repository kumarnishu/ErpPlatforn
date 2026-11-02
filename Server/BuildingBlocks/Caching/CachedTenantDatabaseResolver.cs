using BuildingBlocks.MultiTenancy;

namespace BuildingBlocks.Caching;

/// <summary>Decorates a platform-backed route resolver with short-lived distributed caching.</summary>
public sealed class CachedTenantDatabaseResolver(
    ITenantDatabaseResolver inner,
    ITenantDatabaseRouteCache cache,
    TimeSpan cacheLifetime) : ITenantDatabaseResolver
{
    public async ValueTask<TenantDatabaseRoute> ResolveAsync(
        string tenantId,
        string serviceId,
        CancellationToken cancellationToken = default)
    {
        var cachedRoute = await cache.GetAsync(tenantId, serviceId, cancellationToken);
        if (cachedRoute is not null)
        {
            return cachedRoute;
        }

        var route = await inner.ResolveAsync(tenantId, serviceId, cancellationToken);
        await cache.SetAsync(route, cacheLifetime, cancellationToken);
        return route;
    }
}
