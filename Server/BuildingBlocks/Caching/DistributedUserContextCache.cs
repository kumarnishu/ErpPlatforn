using BuildingBlocks.Context;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace BuildingBlocks.Caching;

/// <summary>Redis-compatible cache implementation backed by ASP.NET Core IDistributedCache.</summary>
public sealed class DistributedUserContextCache(IDistributedCache cache) : IUserContextCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<UserContext?> GetAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var value = await cache.GetStringAsync(Key(tenantId, userId), cancellationToken);
        return value is null
            ? null
            : JsonSerializer.Deserialize<UserContext>(value, JsonOptions);
    }

    public async ValueTask SetAsync(
        UserContext context,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = lifetime
        };
        if (context.TenantId is null || context.UserId is null)
            throw new ArgumentException("TenantId and UserId are required to cache a user context.", nameof(context));

        var value = JsonSerializer.Serialize(context, JsonOptions);
        await cache.SetStringAsync(Key(context.TenantId, context.UserId), value, options, cancellationToken);
    }

    public async ValueTask RemoveAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default) =>
        await cache.RemoveAsync(Key(tenantId, userId), cancellationToken);

    private static string Key(string tenantId, string userId) =>
        $"user-context:v3:{tenantId}:{userId}";
}
