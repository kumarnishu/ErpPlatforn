using System.Text.Json;
using BuildingBlocks.MultiTenancy;
using Microsoft.Extensions.Caching.Distributed;

namespace BuildingBlocks.Caching;

/// <summary>Caches routing metadata only; database credentials remain in a secret store.</summary>
public sealed class DistributedTenantDatabaseRouteCache(IDistributedCache cache) : ITenantDatabaseRouteCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<TenantDatabaseRoute?> GetAsync(
        string tenantId,
        string serviceId,
        CancellationToken cancellationToken = default)
    {
        var value = await cache.GetStringAsync(Key(tenantId, serviceId), cancellationToken);
        return value is null
            ? null
            : JsonSerializer.Deserialize<TenantDatabaseRoute>(value, JsonOptions);
    }

    public async ValueTask SetAsync(
        TenantDatabaseRoute route,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = lifetime
        };
        var value = JsonSerializer.Serialize(route, JsonOptions);
        await cache.SetStringAsync(Key(route.TenantId, route.ServiceId), value, options, cancellationToken);
    }

    public async ValueTask RemoveAsync(
        string tenantId,
        string serviceId,
        CancellationToken cancellationToken = default) =>
        await cache.RemoveAsync(Key(tenantId, serviceId), cancellationToken);

    private static string Key(string tenantId, string serviceId) =>
        $"tenant-db-route:v1:{tenantId}:{serviceId}";
}
