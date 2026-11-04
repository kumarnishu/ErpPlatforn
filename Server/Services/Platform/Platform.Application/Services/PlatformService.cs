using System.Linq.Expressions;
using BuildingBlocks.Context;
using BuildingBlocks.Http;
using Platform.Application.Abstractions;
using Platform.Application.Contracts;
using Platform.Domain;

namespace Platform.Application.Services;

// Coordinates validation, authorization, and repository operations for Platform requests.
/// <summary>Applies Platform access rules before delegating database work to the repository.</summary>
public sealed class PlatformService(IPlatformRepository repository, IUserContext userContext) : IPlatformService
{
    // The repository intentionally returns one entity at a time; load its parents by ID for access checks.
    private async Task<(ProductFeature? Feature, ProductModule? Module, Product? Product)> GetActionPathAsync(ProductAction action, CancellationToken cancellationToken)
    {
        var feature = await repository.FindAsync<ProductFeature>(x => x.Id == action.ProductFeatureId, cancellationToken);
        if (feature is null) return (null, null, null);
        var module = await repository.FindAsync<ProductModule>(x => x.Id == feature.ProductModuleId, cancellationToken);
        if (module is null) return (feature, null, null);
        var product = await repository.FindAsync<Product>(x => x.Id == module.ProductId, cancellationToken);
        return (feature, module, product);
    }

    public void RequirePlatformAdmin()
    {
        // Platform administration is supplied by the token role claim, not by a separate platform-user table.
        if (!userContext.IsAuthenticated)
            throw new ApiException(401, "UNAUTHENTICATED", "Authentication is required.");
        if (!userContext.Roles.Contains("PlatformAdmin", StringComparer.Ordinal))
            throw new ApiException(403, "FORBIDDEN", "Platform administrator access is required.");
    }

    private string RequireUserId()
    {
        if (!userContext.IsAuthenticated || string.IsNullOrWhiteSpace(userContext.UserId))
            throw new ApiException(401, "UNAUTHENTICATED", "Authentication is required.");
        return userContext.UserId;
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync<TEntity>(Expression<Func<TEntity, bool>>? predicate, int page, int pageSize, CancellationToken cancellationToken = default) where TEntity : class
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw new ApiException(400, "INVALID_PAGINATION", "Page must be positive and pageSize must be between 1 and 100.");
        return await repository.ListAsync(predicate, (page - 1) * pageSize, pageSize, cancellationToken);
    }

    public Task<TEntity?> FindAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class =>
        repository.FindAsync(predicate, cancellationToken);

    public async Task<TEntity> CreateCatalogItemAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
    {
        RequirePlatformAdmin();
        // Keys are unique within their owning catalog scope: globally for products, then within each parent.
        var duplicate = entity switch
        {
            Product value => await repository.ExistsAsync<Product>(x => x.Key == value.Key, cancellationToken),
            ProductModule value => await repository.ExistsAsync<ProductModule>(x => x.ProductId == value.ProductId && x.Key == value.Key, cancellationToken),
            ProductFeature value => await repository.ExistsAsync<ProductFeature>(x => x.ProductModuleId == value.ProductModuleId && x.Key == value.Key, cancellationToken),
            ProductAction value => await repository.ExistsAsync<ProductAction>(x => x.ProductFeatureId == value.ProductFeatureId && x.Key == value.Key, cancellationToken),
            _ => false
        };
        if (duplicate)
            throw new ApiException(409, "CATALOG_KEY_EXISTS", "A catalog item with that key already exists in this scope.");

        switch (entity)
        {
            // New catalog items can only be added under active parents.
            case ProductModule module when !await repository.ExistsAsync<Product>(x => x.Id == module.ProductId && x.Status == CatalogStatus.Active, cancellationToken):
                throw new ApiException(404, "PRODUCT_NOT_FOUND", "Active product was not found.");
            case ProductFeature feature when !await repository.ExistsAsync<ProductModule>(x => x.Id == feature.ProductModuleId && x.Status == CatalogStatus.Active, cancellationToken):
                throw new ApiException(404, "MODULE_NOT_FOUND", "Active product module was not found.");
            case ProductAction action when !await repository.ExistsAsync<ProductFeature>(x => x.Id == action.ProductFeatureId && x.Status == CatalogStatus.Active, cancellationToken):
                throw new ApiException(404, "FEATURE_NOT_FOUND", "Active product feature was not found.");
        }

        await repository.AddAsync(entity, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        RequirePlatformAdmin();
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<Tenant> CreateTenantAsync(string key, string name, IReadOnlyList<int> productIds, CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(name))
            throw new ApiException(400, "INVALID_TENANT", "Tenant key and name are required.");
        key = key.Trim();
        name = name.Trim();
        var ids = productIds.Distinct().ToArray();
        if (ids.Length == 0)
            throw new ApiException(400, "PRODUCT_REQUIRED", "Select at least one product.");
        if (ids.Length != productIds.Count)
            throw new ApiException(400, "DUPLICATE_PRODUCT", "A product may be selected only once.");
        if (await repository.ExistsAsync<Tenant>(x => x.Key == key, cancellationToken))
            throw new ApiException(409, "TENANT_KEY_EXISTS", "Tenant key is already in use.");
        var activeProducts = await repository.ListAsync<Product>(x => ids.Contains(x.Id) && x.Status == CatalogStatus.Active, 0, ids.Length, cancellationToken);
        if (activeProducts.Count != ids.Length)
            throw new ApiException(400, "INVALID_PRODUCT", "Every selected product must exist and be active.");

        // Save the tenant, signup user's membership, and initial subscriptions together as one EF unit of work.
        var tenant = new Tenant { Key = key, Name = name, Status = TenantStatus.Active };
        tenant.Members.Add(new TenantMembership { UserId = userId, Status = TenantMembershipStatus.Active, Tenant = tenant });
        foreach (var productId in ids)
            tenant.ProductSubscriptions.Add(new TenantProductSubscription { ProductId = productId, Status = TenantProductSubscriptionStatus.Active, Tenant = tenant });

        await repository.AddAsync(tenant, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return tenant;
    }

    public async Task UpdateTenantAsync(int tenantId, string name, TenantStatus status, CancellationToken cancellationToken = default)
    {
        RequirePlatformAdmin();
        if (string.IsNullOrWhiteSpace(name))
            throw new ApiException(400, "INVALID_TENANT", "Tenant name is required.");
        var tenant = await repository.FindAsync<Tenant>(x => x.Id == tenantId, cancellationToken)
            ?? throw new ApiException(404, "TENANT_NOT_FOUND", "Tenant was not found.");
        if (status == TenantStatus.Active)
        {
            // A suspended tenant can be reactivated only after it has a current active product subscription.
            var activeProducts = await repository.ListAsync<TenantProductSubscription>(
                x => x.TenantId == tenantId && x.Status == TenantProductSubscriptionStatus.Active && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > DateTimeOffset.UtcNow),
                0, 1, cancellationToken);
            if (activeProducts.Count == 0)
                throw new ApiException(409, "PRODUCT_REQUIRED", "An active tenant must have at least one active product subscription.");
        }
        tenant.Name = name.Trim();
        tenant.Status = status;
        tenant.ConcurrencyStamp = Guid.NewGuid();
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<TenantProductSubscription> SubscribeAsync(int tenantId, int productId, CancellationToken cancellationToken = default)
    {
        RequirePlatformAdmin();
        var tenant = await repository.FindAsync<Tenant>(x => x.Id == tenantId, cancellationToken)
            ?? throw new ApiException(404, "TENANT_NOT_FOUND", "Tenant was not found.");
        var product = await repository.FindAsync<Product>(x => x.Id == productId && x.Status == CatalogStatus.Active, cancellationToken)
            ?? throw new ApiException(404, "PRODUCT_NOT_FOUND", "Active product was not found.");
        var subscription = await repository.FindAsync<TenantProductSubscription>(x => x.TenantId == tenantId && x.ProductId == productId, cancellationToken);
        if (subscription is null)
        {
            subscription = new TenantProductSubscription { TenantId = tenantId, ProductId = product.Id, Tenant = tenant, Product = product };
            await repository.AddAsync(subscription, cancellationToken);
        }
        else
        {
            // Reuse the tenant/product row so the unique pair remains stable after cancellation or suspension.
            subscription.Status = TenantProductSubscriptionStatus.Active;
            subscription.SubscribedAtUtc = DateTimeOffset.UtcNow;
            subscription.ExpiresAtUtc = null;
        }

        tenant.ConcurrencyStamp = Guid.NewGuid();
        await repository.SaveChangesAsync(cancellationToken);
        return subscription;
    }

    public async Task UpdateSubscriptionAsync(int tenantId, int productId, TenantProductSubscriptionStatus status, DateTimeOffset? expiresAtUtc, CancellationToken cancellationToken = default)
    {
        RequirePlatformAdmin();
        var subscription = await repository.FindAsync<TenantProductSubscription>(x => x.TenantId == tenantId && x.ProductId == productId, cancellationToken)
            ?? throw new ApiException(404, "SUBSCRIPTION_NOT_FOUND", "Tenant product subscription was not found.");
        var tenant = await repository.FindAsync<Tenant>(x => x.Id == tenantId, cancellationToken)
            ?? throw new ApiException(404, "TENANT_NOT_FOUND", "Tenant was not found.");
        if (status == TenantProductSubscriptionStatus.Active && expiresAtUtc.HasValue && expiresAtUtc.Value <= DateTimeOffset.UtcNow)
            throw new ApiException(400, "INVALID_EXPIRATION", "An active subscription must expire in the future.");
        if (tenant.Status == TenantStatus.Active && (status != TenantProductSubscriptionStatus.Active || (expiresAtUtc.HasValue && expiresAtUtc.Value <= DateTimeOffset.UtcNow)))
        {
            // Count only other current subscriptions before allowing this one to stop granting product access.
            var others = await repository.ListAsync<TenantProductSubscription>(
                x => x.TenantId == tenantId && x.ProductId != productId && x.Status == TenantProductSubscriptionStatus.Active && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > DateTimeOffset.UtcNow),
                0, 2, cancellationToken);
            if (others.Count == 0)
                throw new ApiException(409, "LAST_ACTIVE_PRODUCT", "An active tenant must retain at least one active product subscription.");
        }
        subscription.Status = status;
        subscription.ExpiresAtUtc = expiresAtUtc;
        // Touching this token makes concurrent subscription changes for one tenant conflict instead of removing its last product.
        tenant.ConcurrencyStamp = Guid.NewGuid();
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<TenantMembership> AddMembershipAsync(int tenantId, string userId, CancellationToken cancellationToken = default)
    {
        RequirePlatformAdmin();
        if (string.IsNullOrWhiteSpace(userId))
            throw new ApiException(400, "USER_ID_REQUIRED", "User ID is required.");
        userId = userId.Trim();
        var tenant = await repository.FindAsync<Tenant>(x => x.Id == tenantId, cancellationToken)
            ?? throw new ApiException(404, "TENANT_NOT_FOUND", "Tenant was not found.");
        var existing = await repository.FindAsync<TenantMembership>(x => x.TenantId == tenantId && x.UserId == userId, cancellationToken);
        if (existing is not null)
            throw new ApiException(409, "MEMBERSHIP_EXISTS", "The user already has a membership in this tenant.");
        var membership = new TenantMembership { TenantId = tenantId, UserId = userId, Tenant = tenant };
        await repository.AddAsync(membership, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return membership;
    }

    public async Task UpdateMembershipAsync(int tenantId, int membershipId, TenantMembershipStatus status, CancellationToken cancellationToken = default)
    {
        RequirePlatformAdmin();
        var membership = await repository.FindAsync<TenantMembership>(x => x.Id == membershipId && x.TenantId == tenantId, cancellationToken)
            ?? throw new ApiException(404, "MEMBERSHIP_NOT_FOUND", "Tenant membership was not found.");
        membership.Status = status;
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<TenantUserAction> AssignActionAsync(int tenantId, int membershipId, int actionId, CancellationToken cancellationToken = default)
    {
        RequirePlatformAdmin();
        var membership = await repository.FindAsync<TenantMembership>(x => x.Id == membershipId && x.TenantId == tenantId && x.Status == TenantMembershipStatus.Active, cancellationToken)
            ?? throw new ApiException(404, "MEMBERSHIP_NOT_FOUND", "Active tenant membership was not found.");
        var tenant = await repository.FindAsync<Tenant>(x => x.Id == tenantId && x.Status == TenantStatus.Active, cancellationToken)
            ?? throw new ApiException(404, "TENANT_NOT_FOUND", "Active tenant was not found.");
        var action = await repository.FindAsync<ProductAction>(x => x.Id == actionId && x.Status == CatalogStatus.Active, cancellationToken)
            ?? throw new ApiException(404, "ACTION_NOT_FOUND", "Active product action was not found.");
        var (feature, module, product) = await GetActionPathAsync(action, cancellationToken);
        if (feature is null || module is null || product is null || feature.Status != CatalogStatus.Active || module.Status != CatalogStatus.Active || product.Status != CatalogStatus.Active)
            throw new ApiException(404, "ACTION_NOT_FOUND", "Active product action was not found.");
        var productId = product.Id;
        var subscription = await repository.FindAsync<TenantProductSubscription>(x => x.TenantId == tenantId && x.ProductId == productId && x.Status == TenantProductSubscriptionStatus.Active && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > DateTimeOffset.UtcNow), cancellationToken);
        if (subscription is null)
            throw new ApiException(409, "PRODUCT_NOT_SUBSCRIBED", "The tenant must have an active subscription to the action's product.");

        // Grants are scoped to the membership and exact action; repeating a grant simply reactivates its row.
        var assignment = await repository.FindAsync<TenantUserAction>(x => x.TenantMembershipId == membershipId && x.ProductActionId == actionId, cancellationToken);
        if (assignment is null)
        {
            assignment = new TenantUserAction { Membership = membership, Action = action, IsActive = true };
            await repository.AddAsync(assignment, cancellationToken);
        }
        else assignment.IsActive = true;
        await repository.SaveChangesAsync(cancellationToken);
        return assignment;
    }

    public async Task RevokeActionAsync(int tenantId, int membershipId, int actionId, CancellationToken cancellationToken = default)
    {
        RequirePlatformAdmin();
        var assignment = await repository.FindAsync<TenantUserAction>(x => x.TenantMembershipId == membershipId && x.ProductActionId == actionId && x.Membership.TenantId == tenantId, cancellationToken)
            ?? throw new ApiException(404, "ASSIGNMENT_NOT_FOUND", "User action assignment was not found.");
        assignment.IsActive = false;
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthorizationDecision> CheckAccessAsync(int tenantId, string productKey, string moduleKey, string featureKey, string actionKey, CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();
        // Resolve identity only from the validated request context; never accept a user ID from the request body.
        var membership = await repository.FindAsync<TenantMembership>(x => x.TenantId == tenantId && x.UserId == userId, cancellationToken);
        if (membership is null || membership.Status != TenantMembershipStatus.Active)
            return new(false, "membership_inactive");
        var tenant = await repository.FindAsync<Tenant>(x => x.Id == tenantId, cancellationToken);
        if (tenant is null || tenant.Status != TenantStatus.Active)
            return new(false, "tenant_inactive");
        var action = await repository.FindAsync<ProductAction>(x => x.Key == actionKey && x.Feature.Key == featureKey && x.Feature.Module.Key == moduleKey && x.Feature.Module.Product.Key == productKey, cancellationToken);
        if (action is null)
            return new(false, "catalog_item_not_found");
        var (feature, module, product) = await GetActionPathAsync(action, cancellationToken);
        if (action.Status != CatalogStatus.Active || feature is null || module is null || product is null || feature.Status != CatalogStatus.Active || module.Status != CatalogStatus.Active || product.Status != CatalogStatus.Active)
            return new(false, "catalog_item_inactive");
        var productId = product.Id;
        var subscription = await repository.ExistsAsync<TenantProductSubscription>(x => x.TenantId == tenantId && x.ProductId == productId && x.Status == TenantProductSubscriptionStatus.Active && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > DateTimeOffset.UtcNow), cancellationToken);
        if (!subscription)
            return new(false, "product_not_subscribed");
        var assigned = await repository.ExistsAsync<TenantUserAction>(x => x.TenantMembershipId == membership.Id && x.ProductActionId == action.Id && x.IsActive, cancellationToken);
        return assigned ? new(true) : new(false, "action_not_assigned");
    }

    public async Task<IReadOnlyList<Tenant>> ListMyTenantsAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();
        var memberships = await repository.ListAsync<TenantMembership>(x => x.UserId == userId && x.Status == TenantMembershipStatus.Active, 0, 500, cancellationToken);
        var results = new List<Tenant>();
        foreach (var membership in memberships)
        {
            var tenant = await repository.FindAsync<Tenant>(x => x.Id == membership.TenantId && x.Status == TenantStatus.Active, cancellationToken);
            if (tenant is not null) results.Add(tenant);
        }
        return results;
    }

    public async Task<IReadOnlyList<TenantUserAction>> ListMyActionsAsync(int tenantId, int? productId, CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();
        var tenant = await repository.FindAsync<Tenant>(x => x.Id == tenantId && x.Status == TenantStatus.Active, cancellationToken);
        if (tenant is null) return [];
        var membership = await repository.FindAsync<TenantMembership>(x => x.TenantId == tenantId && x.UserId == userId && x.Status == TenantMembershipStatus.Active, cancellationToken);
        if (membership is null) throw new ApiException(404, "MEMBERSHIP_NOT_FOUND", "Active tenant membership was not found.");
        var assignments = await repository.ListAsync<TenantUserAction>(x => x.TenantMembershipId == membership.Id && x.IsActive, 0, 500, cancellationToken);
        var results = new List<TenantUserAction>();
        foreach (var assignment in assignments)
        {
            var action = await repository.FindAsync<ProductAction>(x => x.Id == assignment.ProductActionId && x.Status == CatalogStatus.Active, cancellationToken);
            if (action is null) continue;
            var (feature, module, product) = await GetActionPathAsync(action, cancellationToken);
            if (feature is null || module is null || product is null || feature.Status != CatalogStatus.Active || module.Status != CatalogStatus.Active || product.Status != CatalogStatus.Active || (productId.HasValue && product.Id != productId.Value)) continue;
            var subscribed = await repository.ExistsAsync<TenantProductSubscription>(x => x.TenantId == tenantId && x.ProductId == product.Id && x.Status == TenantProductSubscriptionStatus.Active && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > DateTimeOffset.UtcNow), cancellationToken);
            if (subscribed) results.Add(assignment);
        }
        return results;
    }
}
