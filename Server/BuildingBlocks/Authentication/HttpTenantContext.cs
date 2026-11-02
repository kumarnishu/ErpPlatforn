using BuildingBlocks.Context;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Authentication;

internal sealed class HttpTenantContext(IHttpContextAccessor accessor) : ITenantContext
{
    public string? TenantId => accessor.HttpContext?.User
        .FindFirst(AuthenticationClaimTypes.TenantId)?.Value;
}
