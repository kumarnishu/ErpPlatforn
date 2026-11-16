using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace BuildingBlocks.Http;

public static class SwaggerExtensions
{
    public static IServiceCollection AddApiSwagger(
        this IServiceCollection services,
        string title,
        string description,
        string version = "v1")
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(version, new OpenApiInfo
            {
                Title = title,
                Version = version,
                Description = description
            });
        });

        return services;
    }

    public static WebApplication UseApiSwagger(
        this WebApplication app,
        string title,
        string version = "v1")
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"/swagger/{version}/swagger.json", $"{title} {version}");
            options.RoutePrefix = "swagger";
        });

        return app;
    }
}
