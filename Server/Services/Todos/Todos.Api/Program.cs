using BuildingBlocks.Caching;
using BuildingBlocks.JWT;
using BuildingBlocks.Middleware;
using BuildingBlocks.Swagger;
using BuildingBlocks.UserContext;
using Microsoft.EntityFrameworkCore;
using Todos.Api;

var builder = WebApplication.CreateBuilder(args);
var audience = builder.Configuration["Authentication:Audience"];
var issuer = builder.Configuration["Authentication:Issuer"];
var signingKey = builder.Configuration["Authentication:SigningKey"];
var authenticationConfigured = !string.IsNullOrWhiteSpace(signingKey);

builder.Services.AddSharedJwtAuthentication(audience, issuer, signingKey);
builder.Services.AddSharedDistributedCaches(builder.Configuration);
builder.Services.AddUserContext();
builder.Services.AddSingleton<TenantDatabaseConnectionFactory>();
builder.Services.AddSingleton<TenantDatabaseManager>();
builder.Services.AddDbContext<TodoDbContext>((services, options) =>
{
    var tenantKey = services.GetRequiredService<IUserContextAccessor>().Current?.TenantKey ?? "D1";
    var connections = services.GetRequiredService<TenantDatabaseConnectionFactory>();
    options.UseNpgsql(connections.GetTenantConnectionString(tenantKey));
});
builder.Services.AddApiSwagger("Todos API", "Tenant-scoped todo management API");

var app = builder.Build();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseApiSwagger("Todos API");
app.UseAuthentication();
app.UseMiddleware<UserContextMiddleware>();
app.UseMiddleware<TenantDatabaseMiddleware>();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { service = "todos", status = "ok" }));

if (authenticationConfigured)
{
    app.MapTodoEndpoints();
}
else
{
    app.Map("/api/v1/todos/{**path}", (HttpContext context) =>
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return Results.Problem("Configure an issuer, audience, and signing key to use the Todos API.");
    });
}

app.Run();
