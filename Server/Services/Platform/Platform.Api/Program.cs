using BuildingBlocks.ApiResponses;
using BuildingBlocks.Caching;
using BuildingBlocks.JWT;
using BuildingBlocks.Middleware;
using BuildingBlocks.Swagger;
using BuildingBlocks.UserContext;

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
builder.Services.AddApiSwagger("Platform API", "Central platform API");

var app = builder.Build();

// Register exception handling outside the rest of the pipeline.
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseApiSwagger("Platform API");
app.UseAuthentication();
app.UseMiddleware<UserContextMiddleware>();
app.UseAuthorization();

app.MapGet("/health", (HttpContext context) =>
    Results.Ok(ApiResponse<object>.Success(
        new { service = "platform", status = "ok" },
        context.TraceIdentifier)));

if (authenticationConfigured)
{
    app.MapGet("/api/v1/user-context", (IUserContextAccessor accessor, HttpContext context) =>
        Results.Ok(ApiResponse<UserContext?>.Success(accessor.Current, context.TraceIdentifier)))
        .RequireAuthorization();
}
else
{
    app.MapGet("/api/v1/user-context", (HttpContext context) =>
        Results.Json(
            ApiResponse<object>.Failure(
                new ApiError("AUTHENTICATION_NOT_CONFIGURED", "Configure an authentication authority or signing key."),
                context.TraceIdentifier),
            statusCode: StatusCodes.Status503ServiceUnavailable));
}

app.Run();
