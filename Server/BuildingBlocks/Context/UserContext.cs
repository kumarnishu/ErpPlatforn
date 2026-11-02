namespace BuildingBlocks.Context;

public sealed record UserContext(
    string? UserId,
    bool IsAuthenticated,
    IReadOnlyCollection<string> Roles) : IUserContext;
