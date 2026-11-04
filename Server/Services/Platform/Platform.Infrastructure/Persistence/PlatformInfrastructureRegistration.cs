using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Abstractions;

namespace Platform.Infrastructure.Persistence;

// Keeps provider-specific DbContext and repository registration in Infrastructure.
/// <summary>Registers SQL Server persistence for the Platform service.</summary>
public static class PlatformInfrastructureRegistration
{
    public static IServiceCollection AddPlatformInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        services.AddDbContext<PlatformDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IPlatformRepository, EfPlatformRepository>();
        return services;
    }
}
