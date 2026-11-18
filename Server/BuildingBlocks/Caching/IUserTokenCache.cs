namespace BuildingBlocks.Caching;

public interface IUserTokenCache
{
    ValueTask<UserToken?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default);
    ValueTask<bool> TryAddAsync(UserToken userToken, CancellationToken cancellationToken = default);
    ValueTask RemoveAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
