using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Abstractions;
using Platform.Infrastructure.Persistence;
using Platform.Infrastructure.Security;

namespace Platform.Infrastructure;

public static class PlatformInfrastructureRegistration
{
    public static IServiceCollection AddPlatformInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PlatformDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ITenantSignupRepository, TenantSignupRepository>();
        services.AddSingleton<IPasswordHashService, PasswordHashService>();
        return services;
    }
}
