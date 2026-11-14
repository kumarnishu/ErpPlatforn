using Platform.Application.Contracts;

namespace Platform.Application.Services;

public interface ITenantSignupService
{
    Task<TenantSignupResult> SignUpAsync(TenantSignupRequest request, CancellationToken cancellationToken);
}
