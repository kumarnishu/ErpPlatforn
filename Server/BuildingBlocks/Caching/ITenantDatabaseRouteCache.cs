using BuildingBlocks.MultiTenancy;

namespace BuildingBlocks.Caching;

public interface ITenantDatabaseRouteCache
{
    ValueTask<TenantDatabaseRoute?> GetAsync(
        string tenantId,
        string serviceId,
        CancellationToken cancellationToken = default);

    ValueTask SetAsync(
        TenantDatabaseRoute route,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default);

    ValueTask RemoveAsync(
        string tenantId,
        string serviceId,
        CancellationToken cancellationToken = default);
}
