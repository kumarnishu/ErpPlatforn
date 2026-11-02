using System.Security.Claims;
using BuildingBlocks.Context;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Authentication;

internal sealed class HttpUserContext(IHttpContextAccessor accessor) : IUserContext
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public string? UserId => Principal?.FindFirst(AuthenticationClaimTypes.UserId)?.Value;
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public IReadOnlyCollection<string> Roles => Principal?
        .FindAll(AuthenticationClaimTypes.Role)
        .Select(claim => claim.Value)
        .Distinct(StringComparer.Ordinal)
        .ToArray() ?? [];
}
