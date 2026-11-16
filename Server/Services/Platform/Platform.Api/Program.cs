using BuildingBlocks.Http;
using Microsoft.EntityFrameworkCore;
using Platform.Application.Services;
using Platform.Infrastructure;
using Platform.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddApiSwagger("Platform API", "Tenant signup API");

var connectionString = builder.Configuration.GetConnectionString("PlatformDatabase")
    ?? builder.Configuration["PLATFORM_DB_CONNECTION"];

if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddPlatformInfrastructure(connectionString);
    builder.Services.AddScoped<ITenantSignupService, TenantSignupService>();
}

var app = builder.Build();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseApiSwagger("Platform API");
app.MapGet("/health", () => Results.Ok(ApiResponse<object>.Success(new { service = "platform", status = "ok" }, "health-check")));
if (!string.IsNullOrWhiteSpace(connectionString))
{
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        await db.Database.MigrateAsync();
    }

    app.MapControllers();
}
else
{
    app.Map("/api/v1/{**path}", (HttpContext context) =>
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return Results.Json(ApiResponse<object>.Failure(
            new ApiError("PLATFORM_DATABASE_NOT_CONFIGURED", "Configure the PlatformDatabase connection string to use signup."),
            context.TraceIdentifier));
    });
}

app.Run();
