using Platform.Application.Contracts;

namespace Platform.Application.Services;

/// <summary>Account use cases for tenant signup, tenant-scoped login, and tenant-user creation.</summary>
public interface IPlatformAuthService
{
    Task<(Platform.Domain.Tenant Tenant, Platform.Domain.TenantMembership Membership, IReadOnlyList<Platform.Domain.TenantProductSubscription> Subscriptions)> RegisterTenantAdminAsync(RegisterTenantAdminRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<TenantUserCreatedResponse> CreateTenantUserAsync(int tenantId, CreateTenantUserRequest request, CancellationToken cancellationToken = default);
}
