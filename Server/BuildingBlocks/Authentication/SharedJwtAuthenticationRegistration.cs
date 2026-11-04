using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BuildingBlocks.Authentication;

/// <summary>Registers shared JWT bearer validation for service APIs.</summary>
public static class SharedJwtAuthenticationRegistration
{
    /// <summary>Validates tokens from the configured issuer and audience, or leaves the API unauthenticated when unset.</summary>
    public static IServiceCollection AddSharedJwtAuthentication(
        this IServiceCollection services,
        string? authority,
        string? audience,
        string? issuer = null,
        string? signingKey = null)
    {
        var hasExternalIssuer = !string.IsNullOrWhiteSpace(authority);
        var hasLocalKey = !string.IsNullOrWhiteSpace(signingKey);
        if (hasExternalIssuer && hasLocalKey)
            throw new InvalidOperationException("Configure either Authentication:Authority or Authentication:SigningKey, not both.");
        if (hasExternalIssuer && string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("Authentication:Audience is required with Authentication:Authority.");
        if (hasLocalKey && (string.IsNullOrWhiteSpace(audience) || string.IsNullOrWhiteSpace(issuer)))
            throw new InvalidOperationException("Authentication:Issuer and Authentication:Audience are required with Authentication:SigningKey.");

        if (!hasExternalIssuer && !hasLocalKey)
        {
            services.AddAuthentication();
        }
        else if (hasExternalIssuer)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authority;
                    options.Audience = audience;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = AuthenticationClaimTypes.UserId,
                        RoleClaimType = AuthenticationClaimTypes.Role
                    };
                });
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
                    NameClaimType = AuthenticationClaimTypes.UserId,
                    RoleClaimType = AuthenticationClaimTypes.Role,
                    ClockSkew = TimeSpan.FromSeconds(30)
                });
        }

        services.AddAuthorization();
        return services;
    }
}

internal static class PlatformApiSigningKey
{
    public static byte[] Decode(string value)
    {
        try { return Convert.FromBase64String(value); }
        catch (FormatException) { return System.Text.Encoding.UTF8.GetBytes(value); }
    }
}
