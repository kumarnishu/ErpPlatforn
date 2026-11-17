using BuildingBlocks.Context;

namespace BuildingBlocks.Caching;

public interface IUserContextCache
{
    ValueTask<UserContext?> GetAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default);

    ValueTask SetAsync(
        UserContext context,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default);

    ValueTask RemoveAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default);
}
