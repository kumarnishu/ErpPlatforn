using System.Globalization;
using System.Security.Claims;
using BuildingBlocks.ApiResponses;
using BuildingBlocks.Caching;
using BuildingBlocks.JWT;
using BuildingBlocks.UserContext;
using Microsoft.AspNetCore.Http;
using UserContextModel = BuildingBlocks.UserContext.UserContext;

namespace BuildingBlocks.Middleware;

/// <summary>Builds a request-scoped UserContext from an authenticated JWT principal.</summary>
public sealed class UserContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IUserContextAccessor userContextAccessor,
        IUserTokenCache userTokenCache)
    {
        var principal = context.User;
        if (principal.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        if (!TryCreateUserContext(principal, out var userContext))
            throw Unauthorized();

        // Check the server-side session and token version before accepting the claims.
        var cachedToken = await userTokenCache.GetAsync(userContext!.SessionId, context.RequestAborted);
        if (cachedToken is null
            || cachedToken.UserId != userContext.UserId
            || cachedToken.TenantId != userContext.TenantId
            || cachedToken.TokenVersion != userContext.TokenVersion)
        {
            throw Unauthorized();
        }

        userContextAccessor.Current = userContext;
        await next(context);
    }

    private static ApiException Unauthorized() =>
        new(StatusCodes.Status401Unauthorized, "INVALID_SESSION", "The token is invalid or the session is no longer active.");

    private static bool TryCreateUserContext(ClaimsPrincipal principal, out UserContextModel? userContext)
    {
        // Build context only from claims in the authenticated principal.
        userContext = null;
        if (!TryGuidClaim(principal, JwtClaimTypes.UserId, out var userId)
            || !TryGuidClaim(principal, JwtClaimTypes.TenantId, out var tenantId)
            || !TryGuidClaim(principal, JwtClaimTypes.SessionId, JwtClaimTypes.JwtSessionId, out var sessionId)
            || !TryBoolClaim(principal, JwtClaimTypes.IsUserActive, out var isUserActive)
            || !TryBoolClaim(principal, JwtClaimTypes.IsTenantActive, out var isTenantActive)
            || !TryBoolClaim(principal, JwtClaimTypes.TokenVersion, out var tokenVersion)
            || !TrySubscriptionExpiry(principal, out var subscriptionExpiry))
        {
            return false;
        }

        var tenantKey = principal.FindFirst(JwtClaimTypes.TenantKey)?.Value;
        var email = principal.FindFirst(JwtClaimTypes.Email)?.Value;
        var role = principal.FindFirst(JwtClaimTypes.Role)?.Value;
        if (tenantKey is null || email is null || role is null)
            return false;

        try
        {
            userContext = new UserContextModel(
                userId,
                tenantId,
                tenantKey,
                email,
                isUserActive,
                isTenantActive,
                tokenVersion,
                sessionId,
                role,
                subscriptionExpiry);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool TryGuidClaim(ClaimsPrincipal principal, string claimType, out Guid value) =>
        TryGuidClaim(principal, claimType, null, out value);

    private static bool TryGuidClaim(ClaimsPrincipal principal, string claimType, string? alternateClaimType, out Guid value)
    {
        var claim = principal.FindFirst(claimType)?.Value
            ?? (alternateClaimType is null ? null : principal.FindFirst(alternateClaimType)?.Value);
        return Guid.TryParse(claim, out value) && value != Guid.Empty;
    }

    private static bool TryBoolClaim(ClaimsPrincipal principal, string claimType, out bool value) =>
        bool.TryParse(principal.FindFirst(claimType)?.Value, out value);

    private static bool TrySubscriptionExpiry(ClaimsPrincipal principal, out DateTimeOffset value)
    {
        var claim = principal.FindFirst(JwtClaimTypes.SubscriptionExpiry)?.Value;
        if (long.TryParse(claim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixSeconds))
        {
            try
            {
                value = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                value = default;
                return false;
            }
        }

        return DateTimeOffset.TryParse(claim, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out value);
    }
}
