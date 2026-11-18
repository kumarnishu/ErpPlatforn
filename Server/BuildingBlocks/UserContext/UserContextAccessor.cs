namespace BuildingBlocks.UserContext;

internal sealed class UserContextAccessor : IUserContextAccessor
{
    public UserContext? Current { get; set; }
}
