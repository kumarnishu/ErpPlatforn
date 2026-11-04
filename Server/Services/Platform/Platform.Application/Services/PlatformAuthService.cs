using BuildingBlocks.Context;
using BuildingBlocks.Http;
using Platform.Application.Abstractions;
using Platform.Application.Contracts;
using Platform.Domain;

namespace Platform.Application.Services;

/// <summary>Applies account, tenant subscription, and tenant-admin membership rules.</summary>
public sealed class PlatformAuthService(
    IPlatformRepository repository,
    IPasswordCredentialService passwords,
    IPlatformTokenIssuer tokens,
    IUserContext userContext,
    ITenantContext tenantContext) : IPlatformAuthService
{
    public async Task<(Tenant Tenant, TenantMembership Membership, IReadOnlyList<TenantProductSubscription> Subscriptions)> RegisterTenantAdminAsync(RegisterTenantAdminRequest request, CancellationToken cancellationToken = default)
    {
        ValidateAccount(request.DisplayName, request.Email, request.Password);
        var key = Required(request.TenantKey, "tenantKey").ToLowerInvariant();
        var name = Required(request.TenantName, "tenantName");
        var requestedProductIds = request.ProductIds ?? [];
        var ids = requestedProductIds.Distinct().ToArray();
        if (ids.Length == 0) throw new ApiException(400, "PRODUCT_REQUIRED", "Select at least one product.");
        if (ids.Length != requestedProductIds.Count) throw new ApiException(400, "DUPLICATE_PRODUCT", "A product may be selected only once.");
        if (key.Length > 100 || name.Length > 200) throw new ApiException(400, "INVALID_TENANT", "Tenant key must be at most 100 characters and tenant name at most 200 characters.");
        if (await repository.ExistsAsync<Tenant>(x => x.Key == key, cancellationToken)) throw new ApiException(409, "TENANT_KEY_EXISTS", "Tenant key is already in use.");
        var email = request.Email.Trim();
        var normalized = email.ToUpperInvariant();
        if (await repository.ExistsAsync<PlatformUser>(x => x.NormalizedEmail == normalized, cancellationToken)) throw new ApiException(409, "EMAIL_EXISTS", "An account with this email already exists.");
        var products = await repository.ListAsync<Product>(x => ids.Contains(x.Id) && x.Status == CatalogStatus.Active, 0, ids.Length, cancellationToken);
        if (products.Count != ids.Length) throw new ApiException(400, "INVALID_PRODUCT", "Every selected product must exist and be active.");

        var user = new PlatformUser { Email = email, NormalizedEmail = normalized, DisplayName = request.DisplayName.Trim(), PasswordHash = passwords.Hash(request.Password) };
        var tenant = new Tenant { Key = key, Name = name };
        var membership = new TenantMembership { UserId = user.UserId, Role = TenantMembershipRole.TenantAdmin, Tenant = tenant };
        var subscriptions = products.Select(product => new TenantProductSubscription { Tenant = tenant, Product = product }).ToArray();
        tenant.Members.Add(membership);
        foreach (var subscription in subscriptions) tenant.ProductSubscriptions.Add(subscription);
        await repository.AddAsync(user, cancellationToken);
        await repository.AddAsync(tenant, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return (tenant, membership, subscriptions);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || request.TenantId <= 0)
            throw new ApiException(400, "INVALID_LOGIN", "Email, password, and tenantId are required.");
        var normalized = request.Email.Trim().ToUpperInvariant();
        var user = await repository.FindAsync<PlatformUser>(x => x.NormalizedEmail == normalized && x.IsActive, cancellationToken);
        if (user is null || !passwords.Verify(request.Password, user.PasswordHash))
            throw new ApiException(401, "INVALID_CREDENTIALS", "Email or password is incorrect.");
        var tenant = await repository.FindAsync<Tenant>(x => x.Id == request.TenantId && x.Status == TenantStatus.Active, cancellationToken);
        var membership = await repository.FindAsync<TenantMembership>(x => x.UserId == user.UserId && x.TenantId == request.TenantId && x.Status == TenantMembershipStatus.Active, cancellationToken);
        var hasSubscription = await repository.ExistsAsync<TenantProductSubscription>(x => x.TenantId == request.TenantId && x.Status == TenantProductSubscriptionStatus.Active && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > DateTimeOffset.UtcNow), cancellationToken);
        if (tenant is null || membership is null || !hasSubscription)
            throw new ApiException(401, "INVALID_CREDENTIALS", "Email or password is incorrect.");
        return new LoginResponse(tokens.Issue(user, tenant.Id, membership.Role), "Bearer", DateTimeOffset.UtcNow.AddMinutes(60), tenant.Id, membership.Role);
    }

    public async Task<TenantUserCreatedResponse> CreateTenantUserAsync(int tenantId, CreateTenantUserRequest request, CancellationToken cancellationToken = default)
    {
        if (!userContext.IsAuthenticated || string.IsNullOrWhiteSpace(userContext.UserId)) throw new ApiException(401, "UNAUTHENTICATED", "Authentication is required.");
        if (!int.TryParse(tenantContext.TenantId, out var tokenTenantId) || tokenTenantId != tenantId)
            throw new ApiException(403, "TENANT_SCOPE_MISMATCH", "The bearer token is not scoped to this tenant.");
        if (!await repository.ExistsAsync<Tenant>(x => x.Id == tenantId && x.Status == TenantStatus.Active, cancellationToken))
            throw new ApiException(404, "TENANT_NOT_FOUND", "Active tenant was not found.");
        var admin = await repository.FindAsync<TenantMembership>(x => x.TenantId == tenantId && x.UserId == userContext.UserId && x.Status == TenantMembershipStatus.Active && x.Role == TenantMembershipRole.TenantAdmin, cancellationToken);
        if (admin is null) throw new ApiException(403, "FORBIDDEN", "An active tenant administrator membership is required.");
        ValidateAccount(request.DisplayName, request.Email, request.Password);
        var email = request.Email.Trim();
        var normalized = email.ToUpperInvariant();
        if (await repository.ExistsAsync<PlatformUser>(x => x.NormalizedEmail == normalized, cancellationToken)) throw new ApiException(409, "EMAIL_EXISTS", "An account with this email already exists.");
        var user = new PlatformUser { Email = email, NormalizedEmail = normalized, DisplayName = request.DisplayName.Trim(), PasswordHash = passwords.Hash(request.Password) };
        var membership = new TenantMembership { TenantId = tenantId, UserId = user.UserId, Role = TenantMembershipRole.TenantUser };
        await repository.AddAsync(user, cancellationToken);
        await repository.AddAsync(membership, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return new TenantUserCreatedResponse(user.UserId, user.Email, user.DisplayName, tenantId, membership.Role);
    }

    private static void ValidateAccount(string displayName, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length > 200) throw new ApiException(400, "INVALID_DISPLAY_NAME", "Display name is required and must be at most 200 characters.");
        if (string.IsNullOrWhiteSpace(email) || email.Trim().Length > 320 || !email.Contains('@')) throw new ApiException(400, "INVALID_EMAIL", "A valid email address is required.");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12 || password.Length > 256) throw new ApiException(400, "INVALID_PASSWORD", "Password must contain between 12 and 256 characters.");
    }

    private static string Required(string value, string field) => string.IsNullOrWhiteSpace(value) ? throw new ApiException(400, "REQUIRED_FIELD", $"{field} is required.") : value.Trim();
}
