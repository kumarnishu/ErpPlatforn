using Platform.Domain;

namespace Platform.Application.Abstractions;

public interface ITenantSignupRepository
{
    Task<bool> TenantKeyExistsAsync(string key, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task AddTenantWithAdminAsync(Tenant tenant, PlatformUser user, TenantMembership membership, CancellationToken cancellationToken);
}
