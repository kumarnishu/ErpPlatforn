using BuildingBlocks.ApiResponses;
using BuildingBlocks.Caching;
using BuildingBlocks.JWT;
using BuildingBlocks.Middleware;
using BuildingBlocks.Swagger;
using BuildingBlocks.UserContext;
using Microsoft.EntityFrameworkCore;
using Platform.Api.Data;
using Platform.Api.Endpoints;
using Platform.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var audience = builder.Configuration["Authentication:Audience"];
var issuer = builder.Configuration["Authentication:Issuer"];
var signingKey = builder.Configuration["Authentication:SigningKey"];
var authenticationConfigured = !string.IsNullOrWhiteSpace(signingKey);

builder.Services.AddSharedJwtAuthentication(audience, issuer, signingKey);
if (!string.IsNullOrWhiteSpace(signingKey) && !string.IsNullOrWhiteSpace(issuer) && !string.IsNullOrWhiteSpace(audience))
{
    byte[] tokenSigningKey;
    try { tokenSigningKey = Convert.FromBase64String(signingKey); }
    catch (FormatException) { tokenSigningKey = System.Text.Encoding.UTF8.GetBytes(signingKey); }
    builder.Services.AddSingleton<IAuthTokenService>(new AuthTokenService(issuer, audience, tokenSigningKey));
}
builder.Services.AddSharedDistributedCaches(builder.Configuration);
builder.Services.AddUserContext();
builder.Services.AddApiSwagger("Platform API", "Central platform API");

var connectionString = builder.Configuration.GetConnectionString("PlatformDatabase");
if (!string.IsNullOrWhiteSpace(connectionString))
    builder.Services.AddDbContext<PlatformDbContext>(options => options.UseNpgsql(connectionString));

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

app.MapAuthEndpoints();
app.MapTenantEndpoints();

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
                new ApiError("AUTHENTICATION_NOT_CONFIGURED", "Configure an issuer, audience, and signing key."),
                context.TraceIdentifier),
            statusCode: StatusCodes.Status503ServiceUnavailable));
}

app.Run();
