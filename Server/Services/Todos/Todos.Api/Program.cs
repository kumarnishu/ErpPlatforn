using BuildingBlocks.Caching;
using BuildingBlocks.JWT;
using BuildingBlocks.Middleware;
using BuildingBlocks.Swagger;
using BuildingBlocks.UserContext;
using Todos.Api;

var builder = WebApplication.CreateBuilder(args);
var authority = builder.Configuration["Authentication:Authority"];
var audience = builder.Configuration["Authentication:Audience"];
var issuer = builder.Configuration["Authentication:Issuer"];
var signingKey = builder.Configuration["Authentication:SigningKey"];
var authenticationConfigured = !string.IsNullOrWhiteSpace(authority)
    || !string.IsNullOrWhiteSpace(signingKey);

builder.Services.AddSharedJwtAuthentication(authority, audience, issuer, signingKey);
builder.Services.AddSharedDistributedCaches();
builder.Services.AddUserContext();
builder.Services.AddSingleton<TodoStore>();
builder.Services.AddApiSwagger("Todos API", "Tenant-scoped todo management API");

var app = builder.Build();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseApiSwagger("Todos API");
app.UseAuthentication();
app.UseMiddleware<UserContextMiddleware>();
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
        return Results.Problem("Configure an authentication authority or signing key to use the Todos API.");
    });
}

app.Run();
