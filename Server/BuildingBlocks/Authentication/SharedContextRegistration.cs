using BuildingBlocks.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Authentication;

public static class SharedContextRegistration
{
    public static IServiceCollection AddSharedRequestContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddScoped<IUserContext, HttpUserContext>();
        return services;
    }
}
