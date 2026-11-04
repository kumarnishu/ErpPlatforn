using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Platform.Application.Abstractions;
using Platform.Domain;

namespace Platform.Api;

/// <summary>Stores passwords as salted PBKDF2 hashes and signs tenant-scoped bearer tokens.</summary>
public sealed class PlatformSecurityServices(IConfiguration configuration) : IPasswordCredentialService, IPlatformTokenIssuer
{
    private const int Iterations = 600_000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2-sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256" || !int.TryParse(parts[1], out var iterations) || iterations < 100_000) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException) { return false; }
    }

    public string Issue(PlatformUser user, int tenantId, TenantMembershipRole role)
    {
        var key = GetSigningKey();
        var issuer = configuration["Authentication:Issuer"] ?? throw new InvalidOperationException("Authentication:Issuer is required to issue local JWTs.");
        var audience = configuration["Authentication:Audience"] ?? throw new InvalidOperationException("Authentication:Audience is required to issue local JWTs.");
        var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim("tenant_id", tenantId.ToString(System.Globalization.CultureInfo.InvariantCulture))
        };
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(issuer, audience, claims, now, now.AddMinutes(60), credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static byte[] GetSigningKey(IConfiguration configuration)
    {
        var encoded = configuration["Authentication:SigningKey"];
        if (string.IsNullOrWhiteSpace(encoded)) return [];
        try { return Convert.FromBase64String(encoded); }
        catch (FormatException) { return Encoding.UTF8.GetBytes(encoded); }
    }

    private byte[] GetSigningKey()
    {
        var key = GetSigningKey(configuration);
        if (key.Length < 32) throw new InvalidOperationException("Authentication:SigningKey must contain at least 32 bytes (preferably base64 encoded).");
        return key;
    }
}
