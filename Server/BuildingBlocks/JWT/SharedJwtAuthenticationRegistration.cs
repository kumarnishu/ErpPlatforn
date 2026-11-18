using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace BuildingBlocks.JWT;

/// <summary>Registers shared JWT bearer validation for service APIs.</summary>
public static class SharedJwtAuthenticationRegistration
{
    /// <summary>Validates locally issued tokens from the configured issuer and audience, or leaves the API unauthenticated when unset.</summary>
    public static IServiceCollection AddSharedJwtAuthentication(
        this IServiceCollection services,
        string? audience,
        string? issuer,
        string? signingKey)
    {
        var authenticationConfigured = !string.IsNullOrWhiteSpace(signingKey);
        if (authenticationConfigured && (string.IsNullOrWhiteSpace(audience) || string.IsNullOrWhiteSpace(issuer)))
            throw new InvalidOperationException("Authentication:Issuer and Authentication:Audience are required with Authentication:SigningKey.");

        if (!authenticationConfigured)
        {
            services.AddAuthentication();
        }
        else
        {
            var key = PlatformApiSigningKey.Decode(signingKey!);
            if (key.Length < 32) throw new InvalidOperationException("Authentication:SigningKey must contain at least 32 bytes.");
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime = true,
                    NameClaimType = JwtClaimTypes.UserId,
                    RoleClaimType = JwtClaimTypes.Role,
                    ClockSkew = TimeSpan.FromSeconds(30)
                });
        }

        services.AddAuthorization();
        return services;
    }
}

/// <summary>Claim names expected in validated JWTs.</summary>
public static class JwtClaimTypes
{
    public const string UserId = ClaimTypes.NameIdentifier;
    public const string TenantId = "tenant_id";
    public const string TenantName = "tenant_name";
    public const string TenantKey = "tenant_key";
    public const string SessionId = "session_id";
    public const string JwtSessionId = "sid";
    public const string TokenVersion = "token_version";
    public const string Email = ClaimTypes.Email;
    public const string IsUserActive = "isuser_active";
    public const string IsTenantActive = "istenant_active";
    public const string Role = ClaimTypes.Role;
    public const string SubscriptionExpiry = "subscription_expiry";
}

internal static class PlatformApiSigningKey
{
    public static byte[] Decode(string value)
    {
        try { return Convert.FromBase64String(value); }
        catch (FormatException) { return System.Text.Encoding.UTF8.GetBytes(value); }
    }
}
