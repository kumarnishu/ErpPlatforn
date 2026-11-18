using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace BuildingBlocks.Caching;

/// <summary>Stores user token versions in any ASP.NET Core distributed cache provider.</summary>
public sealed class DistributedUserTokenCache(IDistributedCache cache) : IUserTokenCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly SemaphoreSlim WriteLock = new(1, 1);

    public async ValueTask<UserToken?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var value = await cache.GetStringAsync(Key(sessionId), cancellationToken);
        return value is null ? null : JsonSerializer.Deserialize<UserToken>(value, JsonOptions);
    }

    public async ValueTask<bool> TryAddAsync(UserToken userToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userToken);
        if (userToken.UserId == Guid.Empty)
            throw new ArgumentException("UserId must not be an empty UUID.", nameof(userToken));
        if (userToken.TenantId == Guid.Empty)
            throw new ArgumentException("TenantId must not be an empty UUID.", nameof(userToken));
        if (userToken.SessionId == Guid.Empty)
            throw new ArgumentException("SessionId must not be an empty UUID.", nameof(userToken));

        // Prevent duplicate session entries within this app process.
        await WriteLock.WaitAsync(cancellationToken);
        try
        {
            var key = Key(userToken.SessionId);
            // Keep the first token stored for each session ID.
            if (await cache.GetAsync(key, cancellationToken) is not null)
                return false;

            var value = JsonSerializer.Serialize(userToken, JsonOptions);
            await cache.SetStringAsync(key, value, cancellationToken);
            return true;
        }
        finally
        {
            WriteLock.Release();
        }
    }

    public async ValueTask RemoveAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        await cache.RemoveAsync(Key(sessionId), cancellationToken);

    private static string Key(Guid sessionId) => $"user-token:v4:session:{sessionId:N}";
}
