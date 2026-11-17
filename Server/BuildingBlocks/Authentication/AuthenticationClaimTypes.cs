using System.Security.Claims;

namespace BuildingBlocks.Authentication;

/// <summary>Claim names shared by all services after authentication validates a token.</summary>
public static class AuthenticationClaimTypes
{
    public const string UserId = ClaimTypes.NameIdentifier;
    public const string TenantId = "tenant_id";
    public const string Role = ClaimTypes.Role;
}
