using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.UserContext;

public static class UserContextRegistration
{
    public static IServiceCollection AddUserContext(this IServiceCollection services)
    {
        services.TryAddScoped<IUserContextAccessor, UserContextAccessor>();
        return services;
    }
}
