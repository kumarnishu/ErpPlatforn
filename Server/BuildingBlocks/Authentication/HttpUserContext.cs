using System.Security.Claims;
using BuildingBlocks.Context;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Authentication;

internal sealed class HttpUserContext(IHttpContextAccessor accessor) : IUserContext
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public string? UserId => Principal?.FindFirst(AuthenticationClaimTypes.UserId)?.Value;
    public string? TenantId => Principal?.FindFirst(AuthenticationClaimTypes.TenantId)?.Value;
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public string Role => Principal?.FindFirst(AuthenticationClaimTypes.Role)?.Value ?? string.Empty;
}
