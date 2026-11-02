namespace BuildingBlocks.Caching;

/// <summary>Cacheable authorization context. Never put access tokens or secrets in this record.</summary>
public sealed record UserContextSnapshot(
    string TenantId,
    string UserId,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
