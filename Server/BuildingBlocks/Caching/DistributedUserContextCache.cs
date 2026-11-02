using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace BuildingBlocks.Caching;

/// <summary>Redis-compatible cache implementation backed by ASP.NET Core IDistributedCache.</summary>
public sealed class DistributedUserContextCache(IDistributedCache cache) : IUserContextCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<UserContextSnapshot?> GetAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var value = await cache.GetStringAsync(Key(tenantId, userId), cancellationToken);
        return value is null
            ? null
            : JsonSerializer.Deserialize<UserContextSnapshot>(value, JsonOptions);
    }

    public async ValueTask SetAsync(
        UserContextSnapshot snapshot,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = lifetime
        };
        var value = JsonSerializer.Serialize(snapshot, JsonOptions);
        await cache.SetStringAsync(Key(snapshot.TenantId, snapshot.UserId), value, options, cancellationToken);
    }

    public async ValueTask RemoveAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default) =>
        await cache.RemoveAsync(Key(tenantId, userId), cancellationToken);

    private static string Key(string tenantId, string userId) =>
        $"user-context:v1:{tenantId}:{userId}";
}
