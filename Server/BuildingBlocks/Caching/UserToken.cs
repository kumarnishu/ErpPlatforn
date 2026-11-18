namespace BuildingBlocks.Caching;

/// <summary>Minimal user and session state stored for token validation.</summary>
public sealed record UserToken(Guid UserId, Guid TenantId, Guid SessionId, bool TokenVersion);
