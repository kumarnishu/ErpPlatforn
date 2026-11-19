using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BuildingBlocks.JWT;
using UserContextModel = BuildingBlocks.UserContext.UserContext;
using Microsoft.IdentityModel.Tokens;

namespace Platform.Api.Services;

/// <summary>Access and refresh tokens returned after sign-in or refresh.</summary>
public sealed record AuthTokenPair(
    string AccessToken,
    string RefreshToken,
    string RefreshTokenHash,
    DateTimeOffset AccessTokenExpiresAtUtc,
    DateTimeOffset RefreshTokenExpiresAtUtc);

/// <summary>Creates JWT access tokens and secure refresh tokens.</summary>
public interface IAuthTokenService
{
    AuthTokenPair CreateTokenPair(UserContextModel userContext);
    string CreateAccessToken(UserContextModel userContext, DateTimeOffset expiresAtUtc);
    string HashRefreshToken(string refreshToken);
}

public sealed class AuthTokenService(string issuer, string audience, byte[] signingKey) : IAuthTokenService
{
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public AuthTokenPair CreateTokenPair(UserContextModel userContext)
    {
        ArgumentNullException.ThrowIfNull(userContext);

        var now = DateTimeOffset.UtcNow;
        var accessTokenExpiresAt = now.Add(AccessTokenLifetime);
        var refreshTokenExpiresAt = now.Add(RefreshTokenLifetime);
        var refreshToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));

        return new AuthTokenPair(
            CreateAccessToken(userContext, accessTokenExpiresAt),
            refreshToken,
            HashRefreshToken(refreshToken),
            accessTokenExpiresAt,
            refreshTokenExpiresAt);
    }

    public string CreateAccessToken(UserContextModel userContext, DateTimeOffset expiresAtUtc)
    {
        ArgumentNullException.ThrowIfNull(userContext);
        if (expiresAtUtc <= DateTimeOffset.UtcNow)
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc), "Access token expiry must be in the future.");

        var claims = new[]
        {
            new Claim(JwtClaimTypes.UserId, userContext.UserId.ToString()),
            new Claim(JwtClaimTypes.TenantId, userContext.TenantId.ToString()),
            new Claim(JwtClaimTypes.TenantKey, userContext.TenantKey),
            new Claim(JwtClaimTypes.SessionId, userContext.SessionId.ToString()),
            new Claim(JwtClaimTypes.Email, userContext.Email),
            new Claim(JwtClaimTypes.IsUserActive, userContext.IsUserActive.ToString()),
            new Claim(JwtClaimTypes.IsTenantActive, userContext.IsTenantActive.ToString()),
            new Claim(JwtClaimTypes.TokenVersion, userContext.TokenVersion.ToString()),
            new Claim(JwtClaimTypes.Role, userContext.Role),
            new Claim(JwtClaimTypes.SubscriptionExpiry, userContext.SubscriptionExpiry.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture))
        };

        var credentials = new SigningCredentials(new SymmetricSecurityKey(signingKey), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string HashRefreshToken(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }
}
