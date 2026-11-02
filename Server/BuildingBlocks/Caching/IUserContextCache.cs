namespace BuildingBlocks.Caching;

public interface IUserContextCache
{
    ValueTask<UserContextSnapshot?> GetAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default);

    ValueTask SetAsync(
        UserContextSnapshot snapshot,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default);

    ValueTask RemoveAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default);
}
