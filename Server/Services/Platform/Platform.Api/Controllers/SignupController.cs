using BuildingBlocks.Http;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Contracts;
using Platform.Application.Services;

namespace Platform.Api.Controllers;

[ApiController]
[Route("api/v1/signup")]
public sealed class SignupController(ITenantSignupService signupService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SignUp([FromBody] TenantSignupRequest request, CancellationToken cancellationToken)
    {
        var result = await signupService.SignUpAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<TenantSignupResult>.Success(result, HttpContext.TraceIdentifier));
    }
}
