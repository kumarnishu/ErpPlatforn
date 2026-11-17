namespace BuildingBlocks.Context;

/// <summary>Identity and authorization data for the authenticated request.</summary>
public interface IUserContext
{
    string? UserId { get; }
    string? TenantId { get; }
    bool IsAuthenticated { get; }
    string Role { get; }
}
