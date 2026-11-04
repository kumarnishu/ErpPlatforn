using BuildingBlocks.Authentication;
using BuildingBlocks.Http;
using Platform.Api;
using Platform.Application.Abstractions;
using Platform.Application.Services;
using Platform.Infrastructure.Persistence;
using System.Text.Json.Serialization;

// Composes the Platform API, optional JWT validation, and configured SQL Server persistence.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSharedRequestContext();
// Send enum names such as "Active" in JSON requests and responses instead of numeric values.
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSharedJwtAuthentication(
    builder.Configuration["Authentication:Authority"],
    builder.Configuration["Authentication:Audience"],
    builder.Configuration["Authentication:Issuer"],
    builder.Configuration["Authentication:SigningKey"]);
builder.Services.AddScoped<IPasswordCredentialService, PlatformSecurityServices>();
builder.Services.AddScoped<IPlatformTokenIssuer>(services => (IPlatformTokenIssuer)services.GetRequiredService<IPasswordCredentialService>());

var platformConnectionString = builder.Configuration.GetConnectionString("PlatformDatabase")
    ?? builder.Configuration["PLATFORM_DB_CONNECTION"];
var persistenceConfigured = !string.IsNullOrWhiteSpace(platformConnectionString);
// Keep the health endpoint available during setup, but fail data routes closed until a database is configured.
if (persistenceConfigured)
{
    builder.Services.AddPlatformInfrastructure(platformConnectionString!);
    builder.Services.AddScoped<IPlatformService, PlatformService>();
    builder.Services.AddScoped<IPlatformAuthService, PlatformAuthService>();
}

var app = builder.Build();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(ApiResponse<object>.Success(
    new { service = "platform", status = "ok" },
    "health-check")));

if (persistenceConfigured)
{
    app.MapPlatformEndpoints();
}
else
{
    app.Map("/api/v1/{**path}", (HttpContext context) =>
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return Results.Json(ApiResponse<object>.Failure(
            new ApiError("PLATFORM_DATABASE_NOT_CONFIGURED", "Configure the PlatformDatabase connection string to use Platform endpoints."),
            context.TraceIdentifier));
    });
}

app.Run();
