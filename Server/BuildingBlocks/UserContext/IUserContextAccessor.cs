namespace BuildingBlocks.UserContext;

/// <summary>Provides the authenticated user context for the current request.</summary>
public interface IUserContextAccessor
{
    UserContext? Current { get; set; }
}
