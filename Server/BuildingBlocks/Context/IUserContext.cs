namespace BuildingBlocks.Context;

/// <summary>Identity and authorization data for the authenticated request.</summary>
public interface IUserContext
{
    string? UserId { get; }
    bool IsAuthenticated { get; }
    IReadOnlyCollection<string> Roles { get; }
}
