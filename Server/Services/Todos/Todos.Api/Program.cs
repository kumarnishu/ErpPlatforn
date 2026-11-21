using BuildingBlocks.Caching;
using BuildingBlocks.JWT;
using BuildingBlocks.Middleware;
using BuildingBlocks.Swagger;
using BuildingBlocks.UserContext;
using Todos.Core.Data;
using Todos.Api;

var builder = WebApplication.CreateBuilder(args);
var audience = builder.Configuration["Authentication:Audience"];
var issuer = builder.Configuration["Authentication:Issuer"];
var signingKey = builder.Configuration["Authentication:SigningKey"];
var authenticationConfigured = !string.IsNullOrWhiteSpace(signingKey);
var connectionString = builder.Configuration.GetConnectionString("PlatformDatabase");
var databaseConfigured = !string.IsNullOrWhiteSpace(connectionString);

builder.Services.AddSharedJwtAuthentication(audience, issuer, signingKey);
builder.Services.AddSharedDistributedCaches();
builder.Services.AddUserContext();
builder.Services.AddTodosCore();
builder.Services.AddApiSwagger("Todos API", "Tenant-scoped todo management API");

var app = builder.Build();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseApiSwagger("Todos API");
app.UseAuthentication();
app.UseMiddleware<UserContextMiddleware>();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { service = "todos", status = "ok" }));

if (authenticationConfigured && databaseConfigured)
{
    app.MapTodoEndpoints();
}
else
{
    app.Map("/api/v1/todos/{**path}", (HttpContext context) =>
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        var reason = !authenticationConfigured
            ? "Configure an issuer, audience, and signing key to use the Todos API."
            : "Configure ConnectionStrings:PlatformDatabase to use the Todos API.";
        return Results.Problem(reason);
    });
}

app.Run();
