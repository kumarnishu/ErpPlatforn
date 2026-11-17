namespace BuildingBlocks.Context;

public sealed record UserContext(
    string? UserId,
    string? TenantId,
    bool IsAuthenticated,
    string Role) : IUserContext;
