using BuildingBlocks.ApiResponses;
using BuildingBlocks.Caching;
using BuildingBlocks.UserContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.Api.Data;
using Platform.Api.Models;
using Platform.Api.Services;
using UserContextModel = BuildingBlocks.UserContext.UserContext;

namespace Platform.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth");

        group.MapPost("/login", LoginAsync);
        group.MapPost("/refresh", RefreshAsync);
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        [FromServices] PlatformDbContext db,
        IAuthTokenService tokens,
        IUserTokenCache tokenCache,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TenantKey) || request.TenantKey.Length is < 2 or > 4 ||
            string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(context, "INVALID_LOGIN_REQUEST", "Tenant key, email, and password are required.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var tenantKey = request.TenantKey.Trim();
        var user = await db.Users.Include(x => x.Tenant)
            .SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail && x.Tenant!.Key == tenantKey, cancellationToken);

        // Return the same response for unknown accounts and incorrect passwords.
        if (user?.Tenant is not { } tenant ||
            new PasswordHasher<PlatformUser>().VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(context, "INVALID_CREDENTIALS", "The tenant key, email, or password is incorrect.");

        var now = DateTimeOffset.UtcNow;
        var subscriptionExpiry = await db.TenantSubscriptions
            .Where(x => x.TenantId == tenant.Id &&
                        (x.Status == TenantSubscriptionStatus.Active || x.Status == TenantSubscriptionStatus.Trial) &&
                        x.StartsAtUtc <= now && x.CurrentPeriodEndsAtUtc > now)
            .OrderByDescending(x => x.CurrentPeriodEndsAtUtc)
            .Select(x => (DateTimeOffset?)x.CurrentPeriodEndsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (!user.IsActive || !tenant.IsActive || subscriptionExpiry is null)
            return Unauthorized(context, "INACTIVE_ACCOUNT", "The user, tenant, or subscription is inactive.");

        var sessionId = Guid.NewGuid();
        var userContext = new UserContextModel(user.Id, tenant.Id, tenant.Key, user.Email, user.IsActive,
            tenant.IsActive, user.TokenVersion, sessionId, user.Role, subscriptionExpiry.Value);
        var pair = tokens.CreateTokenPair(userContext);
        var session = new UserSession
        {
            Id = sessionId,
            UserId = user.Id,
            TenantId = tenant.Id,
            RefreshTokenHash = pair.RefreshTokenHash,
            CreatedAtUtc = now,
            ExpiresAtUtc = pair.RefreshTokenExpiresAtUtc
        };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.UserSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        if (!await tokenCache.TryAddAsync(new UserToken(user.Id, tenant.Id, sessionId, user.TokenVersion), cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return ServerError(context, "SESSION_CREATION_FAILED", "A session could not be created.");
        }
        await transaction.CommitAsync(cancellationToken);

        return Results.Ok(ApiResponse<AuthTokenResponse>.Success(ToResponse(pair), context.TraceIdentifier));
    }

    private static async Task<IResult> RefreshAsync(
        RefreshRequest request,
        [FromServices] PlatformDbContext db,
        IAuthTokenService tokens,
        IUserTokenCache tokenCache,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(context, "INVALID_REFRESH_TOKEN", "A refresh token is required.");

        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var session = await db.UserSessions.Include(x => x.User).Include(x => x.Tenant)
            .SingleOrDefaultAsync(x => x.RefreshTokenHash == hash, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (session?.User is not { } user || session.Tenant is not { } tenant ||
            session.RevokedAtUtc is not null || session.ExpiresAtUtc <= now ||
            !user.IsActive || !tenant.IsActive || user.TenantId != tenant.Id)
            return Unauthorized(context, "INVALID_REFRESH_TOKEN", "The refresh token is invalid or expired.");

        var cacheEntry = await tokenCache.GetAsync(session.Id, cancellationToken);
        if (cacheEntry is null || cacheEntry.UserId != user.Id || cacheEntry.TenantId != tenant.Id || cacheEntry.TokenVersion != user.TokenVersion)
            return Unauthorized(context, "INVALID_SESSION", "The session is no longer active.");

        var subscriptionExpiry = await db.TenantSubscriptions
            .Where(x => x.TenantId == tenant.Id &&
                        (x.Status == TenantSubscriptionStatus.Active || x.Status == TenantSubscriptionStatus.Trial) &&
                        x.StartsAtUtc <= now && x.CurrentPeriodEndsAtUtc > now)
            .OrderByDescending(x => x.CurrentPeriodEndsAtUtc)
            .Select(x => (DateTimeOffset?)x.CurrentPeriodEndsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (subscriptionExpiry is null)
            return Unauthorized(context, "INACTIVE_ACCOUNT", "The tenant subscription is inactive.");

        var userContext = new UserContextModel(user.Id, tenant.Id, tenant.Key, user.Email, user.IsActive,
            tenant.IsActive, user.TokenVersion, session.Id, user.Role, subscriptionExpiry.Value);
        var pair = tokens.CreateTokenPair(userContext);

        // Compare the hash again during the update so two concurrent refreshes cannot both rotate it.
        var affected = await db.UserSessions
            .Where(x => x.Id == session.Id && x.RefreshTokenHash == hash && x.RevokedAtUtc == null && x.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.RefreshTokenHash, pair.RefreshTokenHash)
                .SetProperty(x => x.ExpiresAtUtc, pair.RefreshTokenExpiresAtUtc)
                .SetProperty(x => x.LastUsedAtUtc, now), cancellationToken);
        if (affected != 1)
            return Unauthorized(context, "INVALID_REFRESH_TOKEN", "The refresh token is invalid or expired.");

        return Results.Ok(ApiResponse<AuthTokenResponse>.Success(ToResponse(pair), context.TraceIdentifier));
    }

    private static async Task<IResult> LogoutAsync(
        IUserContextAccessor accessor,
        [FromServices] PlatformDbContext db,
        IUserTokenCache tokenCache,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var current = accessor.Current;
        if (current is null)
            return Unauthorized(context, "INVALID_SESSION", "The session is no longer active.");

        var now = DateTimeOffset.UtcNow;
        await db.UserSessions.Where(x => x.Id == current.SessionId && x.UserId == current.UserId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.RevokedAtUtc, now), cancellationToken);
        await tokenCache.RemoveAsync(current.SessionId, cancellationToken);
        return Results.NoContent();
    }

    private static AuthTokenResponse ToResponse(AuthTokenPair pair) =>
        new(pair.AccessToken, pair.RefreshToken, pair.AccessTokenExpiresAtUtc, pair.RefreshTokenExpiresAtUtc);

    private static IResult BadRequest(HttpContext context, string code, string message) => Results.BadRequest(
        ApiResponse<object>.Failure(new ApiError(code, message), context.TraceIdentifier));

    private static IResult Unauthorized(HttpContext context, string code, string message) => Results.Json(
        ApiResponse<object>.Failure(new ApiError(code, message), context.TraceIdentifier),
        statusCode: StatusCodes.Status401Unauthorized);

    private static IResult ServerError(HttpContext context, string code, string message) => Results.Json(
        ApiResponse<object>.Failure(new ApiError(code, message), context.TraceIdentifier),
        statusCode: StatusCodes.Status500InternalServerError);
}

public sealed record LoginRequest(string TenantKey, string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    DateTimeOffset RefreshTokenExpiresAtUtc);
