using System.Linq.Expressions;
using Platform.Application.Contracts;
using Platform.Domain;

namespace Platform.Application.Services;

// Application use cases consumed by the API; business rules live in the implementation.
/// <summary>Use cases for product catalog management and tenant access control.</summary>
public interface IPlatformService
{
    /// <summary>Rejects requests without an authenticated PlatformAdmin role.</summary>
    void RequirePlatformAdmin();
    Task<IReadOnlyList<TEntity>> ListAsync<TEntity>(Expression<Func<TEntity, bool>>? predicate, int page, int pageSize, CancellationToken cancellationToken = default) where TEntity : class;
    Task<TEntity?> FindAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class;
    Task<TEntity> CreateCatalogItemAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
    Task SaveAsync(CancellationToken cancellationToken = default);
    /// <summary>Creates a tenant with its signup user and first product subscriptions in one save.</summary>
    Task<Tenant> CreateTenantAsync(string key, string name, IReadOnlyList<int> productIds, CancellationToken cancellationToken = default);
    /// <summary>Prevents a tenant from becoming active until it has a current product subscription.</summary>
    Task UpdateTenantAsync(int tenantId, string name, TenantStatus status, CancellationToken cancellationToken = default);
    /// <summary>Creates or reactivates the tenant's unique subscription row for this product.</summary>
    Task<TenantProductSubscription> SubscribeAsync(int tenantId, int productId, CancellationToken cancellationToken = default);
    /// <summary>Changes a subscription without allowing an active tenant to lose its final active product.</summary>
    Task UpdateSubscriptionAsync(int tenantId, int productId, TenantProductSubscriptionStatus status, DateTimeOffset? expiresAtUtc, CancellationToken cancellationToken = default);
    Task<TenantMembership> AddMembershipAsync(int tenantId, string userId, CancellationToken cancellationToken = default);
    Task UpdateMembershipAsync(int tenantId, int membershipId, TenantMembershipStatus status, CancellationToken cancellationToken = default);
    /// <summary>Grants a feature action only when its tenant and product are currently eligible.</summary>
    Task<TenantUserAction> AssignActionAsync(int tenantId, int membershipId, int actionId, CancellationToken cancellationToken = default);
    Task RevokeActionAsync(int tenantId, int membershipId, int actionId, CancellationToken cancellationToken = default);
    /// <summary>Evaluates the full tenant, membership, subscription, catalog, and assignment access path.</summary>
    Task<AuthorizationDecision> CheckAccessAsync(int tenantId, string productKey, string moduleKey, string featureKey, string actionKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tenant>> ListMyTenantsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantUserAction>> ListMyActionsAsync(int tenantId, int? productId, CancellationToken cancellationToken = default);
}
