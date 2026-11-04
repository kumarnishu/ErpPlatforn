using BuildingBlocks.Context;
using BuildingBlocks.Http;
using Platform.Application.Contracts;
using Platform.Application.Services;
using Platform.Domain;

namespace Platform.Api;

/// <summary>Maps the HTTP routes for product catalog, tenant subscriptions, memberships, and permissions.</summary>
public static class PlatformEndpoints
{
    public static IEndpointRouteBuilder MapPlatformEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");

        // Public signup creates the first tenant admin and all selected product subscriptions in one database save.
        api.MapPost("/auth/register-tenant-admin", async (RegisterTenantAdminRequest request, IPlatformAuthService auth, CancellationToken ct) =>
        {
            var (tenant, membership, subscriptions) = await auth.RegisterTenantAdminAsync(request, ct);
            var result = new TenantRegistrationResponse(ToResponse(tenant), ToResponse(membership), subscriptions.Select(ToResponse).ToArray());
            return Results.Created($"/api/v1/tenants/{tenant.Id}", ApiResponse<TenantRegistrationResponse>.Success(result, Trace()));
        }).AllowAnonymous();

        // Login always selects a tenant so the token carries a single tenant scope and role.
        api.MapPost("/auth/login", async (LoginRequest request, IPlatformAuthService auth, CancellationToken ct) =>
            Results.Ok(ApiResponse<LoginResponse>.Success(await auth.LoginAsync(request, ct), Trace()))).AllowAnonymous();

        // Lets clients verify the subject and roles carried by the JWT accepted by this API.
        api.MapGet("/auth/me", (IUserContext user) =>
        {
            if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId))
                throw new ApiException(401, "UNAUTHENTICATED", "A valid bearer token is required.");
            return Results.Ok(ApiResponse<object>.Success(new { userId = user.UserId, roles = user.Roles }, Trace()));
        });

        // The product catalog is public to read so signup screens can show active products; writes require PlatformAdmin.
        api.MapGet("/products", async (IPlatformService service, CatalogStatus? status, int? page, int? pageSize, CancellationToken ct) =>
        {
            var rows = await service.ListAsync<Product>(x => status == null || x.Status == status, page ?? 1, pageSize ?? 50, ct);
            return Results.Ok(ApiResponse<IReadOnlyList<ProductResponse>>.Success(rows.Select(ToResponse).ToArray(), Trace()));
        });
        api.MapPost("/products", async (CreateProductRequest request, IPlatformService service, CancellationToken ct) =>
        {
            var row = await service.CreateCatalogItemAsync(new Product { Key = Required(request.Key, "key"), Name = Required(request.Name, "name") }, ct);
            return Results.Created($"/api/v1/products/{row.Id}", ApiResponse<ProductResponse>.Success(ToResponse(row), Trace()));
        });
        api.MapGet("/products/{productId:int}", async (int productId, IPlatformService service, CancellationToken ct) =>
        {
            var row = await service.FindAsync<Product>(x => x.Id == productId, ct) ?? throw NotFound("PRODUCT_NOT_FOUND", "Product was not found.");
            return Results.Ok(ApiResponse<ProductResponse>.Success(ToResponse(row), Trace()));
        });
        api.MapPatch("/products/{productId:int}", async (int productId, UpdateProductRequest request, IPlatformService service, CancellationToken ct) =>
        {
            var row = await service.FindAsync<Product>(x => x.Id == productId, ct) ?? throw NotFound("PRODUCT_NOT_FOUND", "Product was not found.");
            row.Name = Required(request.Name, "name"); row.Status = request.Status;
            await service.SaveAsync(ct);
            return Results.Ok(ApiResponse<ProductResponse>.Success(ToResponse(row), Trace()));
        });

        // Modules, features, and actions are nested under their owners to keep every catalog write properly scoped.
        api.MapGet("/products/{productId:int}/modules", async (int productId, IPlatformService service, CatalogStatus? status, int? page, int? pageSize, CancellationToken ct) =>
        {
            var rows = await service.ListAsync<ProductModule>(x => x.ProductId == productId && (status == null || x.Status == status), page ?? 1, pageSize ?? 50, ct);
            return Results.Ok(ApiResponse<IReadOnlyList<ModuleResponse>>.Success(rows.Select(ToResponse).ToArray(), Trace()));
        });
        api.MapPost("/products/{productId:int}/modules", async (int productId, CreateModuleRequest request, IPlatformService service, CancellationToken ct) =>
        {
            var row = await service.CreateCatalogItemAsync(new ProductModule { ProductId = productId, Key = Required(request.Key, "key"), Name = Required(request.Name, "name"), SortOrder = request.SortOrder }, ct);
            return Results.Created($"/api/v1/products/{productId}/modules/{row.Id}", ApiResponse<ModuleResponse>.Success(ToResponse(row), Trace()));
        });
        api.MapGet("/products/{productId:int}/modules/{moduleId:int}", async (int productId, int moduleId, IPlatformService service, CancellationToken ct) =>
        {
            var row = await service.FindAsync<ProductModule>(x => x.Id == moduleId && x.ProductId == productId, ct) ?? throw NotFound("MODULE_NOT_FOUND", "Product module was not found.");
            return Results.Ok(ApiResponse<ModuleResponse>.Success(ToResponse(row), Trace()));
        });
        api.MapPatch("/products/{productId:int}/modules/{moduleId:int}", async (int productId, int moduleId, UpdateModuleRequest request, IPlatformService service, CancellationToken ct) =>
        {
            var row = await service.FindAsync<ProductModule>(x => x.Id == moduleId && x.ProductId == productId, ct) ?? throw NotFound("MODULE_NOT_FOUND", "Product module was not found.");
            row.Name = Required(request.Name, "name"); row.Status = request.Status; row.SortOrder = request.SortOrder;
            await service.SaveAsync(ct);
            return Results.Ok(ApiResponse<ModuleResponse>.Success(ToResponse(row), Trace()));
        });

        api.MapGet("/products/{productId:int}/modules/{moduleId:int}/features", async (int productId, int moduleId, IPlatformService service, CatalogStatus? status, int? page, int? pageSize, CancellationToken ct) =>
        {
            await RequireModule(productId, moduleId, service, ct);
            var rows = await service.ListAsync<ProductFeature>(x => x.ProductModuleId == moduleId && (status == null || x.Status == status), page ?? 1, pageSize ?? 50, ct);
            return Results.Ok(ApiResponse<IReadOnlyList<FeatureResponse>>.Success(rows.Select(ToResponse).ToArray(), Trace()));
        });
        api.MapPost("/products/{productId:int}/modules/{moduleId:int}/features", async (int productId, int moduleId, CreateFeatureRequest request, IPlatformService service, CancellationToken ct) =>
        {
            await RequireModule(productId, moduleId, service, ct);
            var row = await service.CreateCatalogItemAsync(new ProductFeature { ProductModuleId = moduleId, Key = Required(request.Key, "key"), Name = Required(request.Name, "name"), SortOrder = request.SortOrder }, ct);
            return Results.Created($"/api/v1/products/{productId}/modules/{moduleId}/features/{row.Id}", ApiResponse<FeatureResponse>.Success(ToResponse(row), Trace()));
        });
        api.MapGet("/products/{productId:int}/modules/{moduleId:int}/features/{featureId:int}", async (int productId, int moduleId, int featureId, IPlatformService service, CancellationToken ct) =>
        {
            var row = await RequireFeature(productId, moduleId, featureId, service, ct);
            return Results.Ok(ApiResponse<FeatureResponse>.Success(ToResponse(row), Trace()));
        });
        api.MapPatch("/products/{productId:int}/modules/{moduleId:int}/features/{featureId:int}", async (int productId, int moduleId, int featureId, UpdateFeatureRequest request, IPlatformService service, CancellationToken ct) =>
        {
            var row = await RequireFeature(productId, moduleId, featureId, service, ct);
            row.Name = Required(request.Name, "name"); row.Status = request.Status; row.SortOrder = request.SortOrder;
            await service.SaveAsync(ct);
            return Results.Ok(ApiResponse<FeatureResponse>.Success(ToResponse(row), Trace()));
        });

        api.MapGet("/products/{productId:int}/modules/{moduleId:int}/features/{featureId:int}/actions", async (int productId, int moduleId, int featureId, IPlatformService service, CatalogStatus? status, int? page, int? pageSize, CancellationToken ct) =>
        {
            await RequireFeature(productId, moduleId, featureId, service, ct);
            var rows = await service.ListAsync<ProductAction>(x => x.ProductFeatureId == featureId && (status == null || x.Status == status), page ?? 1, pageSize ?? 50, ct);
            return Results.Ok(ApiResponse<IReadOnlyList<ActionResponse>>.Success(rows.Select(ToResponse).ToArray(), Trace()));
        });
        api.MapPost("/products/{productId:int}/modules/{moduleId:int}/features/{featureId:int}/actions", async (int productId, int moduleId, int featureId, CreateActionRequest request, IPlatformService service, CancellationToken ct) =>
        {
            await RequireFeature(productId, moduleId, featureId, service, ct);
            var row = await service.CreateCatalogItemAsync(new ProductAction { ProductFeatureId = featureId, Key = Required(request.Key, "key"), Name = Required(request.Name, "name") }, ct);
            return Results.Created($"/api/v1/products/{productId}/modules/{moduleId}/features/{featureId}/actions/{row.Id}", ApiResponse<ActionResponse>.Success(ToResponse(row), Trace()));
        });
        api.MapGet("/products/{productId:int}/modules/{moduleId:int}/features/{featureId:int}/actions/{actionId:int}", async (int productId, int moduleId, int featureId, int actionId, IPlatformService service, CancellationToken ct) =>
        {
            await RequireFeature(productId, moduleId, featureId, service, ct);
            var row = await service.FindAsync<ProductAction>(x => x.Id == actionId && x.ProductFeatureId == featureId, ct) ?? throw NotFound("ACTION_NOT_FOUND", "Product action was not found.");
            return Results.Ok(ApiResponse<ActionResponse>.Success(ToResponse(row), Trace()));
        });
        api.MapPatch("/products/{productId:int}/modules/{moduleId:int}/features/{featureId:int}/actions/{actionId:int}", async (int productId, int moduleId, int featureId, int actionId, UpdateActionRequest request, IPlatformService service, CancellationToken ct) =>
        {
            await RequireFeature(productId, moduleId, featureId, service, ct);
            var row = await service.FindAsync<ProductAction>(x => x.Id == actionId && x.ProductFeatureId == featureId, ct) ?? throw NotFound("ACTION_NOT_FOUND", "Product action was not found.");
            row.Name = Required(request.Name, "name"); row.Status = request.Status;
            await service.SaveAsync(ct);
            return Results.Ok(ApiResponse<ActionResponse>.Success(ToResponse(row), Trace()));
        });

        api.MapGet("/tenants", async (IPlatformService service, TenantStatus? status, string? search, int? page, int? pageSize, CancellationToken ct) =>
        {
            service.RequirePlatformAdmin();
            var rows = await service.ListAsync<Tenant>(x => (status == null || x.Status == status) && (string.IsNullOrWhiteSpace(search) || x.Key.Contains(search) || x.Name.Contains(search)), page ?? 1, pageSize ?? 50, ct);
            return Results.Ok(ApiResponse<IReadOnlyList<TenantResponse>>.Success(rows.Select(ToResponse).ToArray(), Trace()));
        });
        api.MapGet("/tenants/{tenantId:int}", async (int tenantId, IPlatformService service, CancellationToken ct) =>
        {
            service.RequirePlatformAdmin();
            var row = await service.FindAsync<Tenant>(x => x.Id == tenantId, ct) ?? throw NotFound("TENANT_NOT_FOUND", "Tenant was not found.");
            var subscriptions = await GetActiveSubscriptions(service, tenantId, ct);
            return Results.Ok(ApiResponse<TenantDetailResponse>.Success(new(ToResponse(row), subscriptions), Trace()));
        });
        api.MapPatch("/tenants/{tenantId:int}", async (int tenantId, UpdateTenantRequest request, IPlatformService service, CancellationToken ct) =>
        {
            service.RequirePlatformAdmin();
            var row = await service.FindAsync<Tenant>(x => x.Id == tenantId, ct) ?? throw NotFound("TENANT_NOT_FOUND", "Tenant was not found.");
            await service.UpdateTenantAsync(tenantId, Required(request.Name, "name"), request.Status, ct);
            return Results.Ok(ApiResponse<TenantResponse>.Success(ToResponse(row), Trace()));
        });
        api.MapGet("/me/tenants", async (IPlatformService service, CancellationToken ct) =>
        {
            var rows = await service.ListMyTenantsAsync(ct);
            var details = new List<TenantDetailResponse>();
            foreach (var tenant in rows)
                details.Add(new(ToResponse(tenant), await GetActiveSubscriptions(service, tenant.Id, ct)));
            return Results.Ok(ApiResponse<IReadOnlyList<TenantDetailResponse>>.Success(details, Trace()));
        });

        // Subscription writes preserve the active-tenant minimum-one-product rule in the application service.
        api.MapGet("/tenants/{tenantId:int}/product-subscriptions", async (int tenantId, IPlatformService service, TenantProductSubscriptionStatus? status, int? page, int? pageSize, CancellationToken ct) =>
        {
            service.RequirePlatformAdmin();
            var rows = await service.ListAsync<TenantProductSubscription>(x => x.TenantId == tenantId && (status == null || x.Status == status), page ?? 1, pageSize ?? 50, ct);
            return Results.Ok(ApiResponse<IReadOnlyList<SubscriptionResponse>>.Success(rows.Select(ToResponse).ToArray(), Trace()));
        });
        api.MapPut("/tenants/{tenantId:int}/product-subscriptions/{productId:int}", async (int tenantId, int productId, IPlatformService service, CancellationToken ct) =>
        {
            var row = await service.SubscribeAsync(tenantId, productId, ct);
            return Results.Ok(ApiResponse<SubscriptionResponse>.Success(ToResponse(row), Trace()));
        });
        api.MapGet("/tenants/{tenantId:int}/product-subscriptions/{productId:int}", async (int tenantId, int productId, IPlatformService service, CancellationToken ct) =>
        {
            service.RequirePlatformAdmin();
            var row = await service.FindAsync<TenantProductSubscription>(x => x.TenantId == tenantId && x.ProductId == productId, ct) ?? throw NotFound("SUBSCRIPTION_NOT_FOUND", "Tenant product subscription was not found.");
            return Results.Ok(ApiResponse<SubscriptionResponse>.Success(ToResponse(row), Trace()));
        });
        api.MapPatch("/tenants/{tenantId:int}/product-subscriptions/{productId:int}", async (int tenantId, int productId, UpdateSubscriptionRequest request, IPlatformService service, CancellationToken ct) =>
        {
            await service.UpdateSubscriptionAsync(tenantId, productId, request.Status, request.ExpiresAtUtc, ct);
            return Results.NoContent();
        });

        // Memberships and action grants are managed by platform administrators; tenant users can only read their own access.
        api.MapGet("/tenants/{tenantId:int}/memberships", async (int tenantId, IPlatformService service, TenantMembershipStatus? status, string? search, int? page, int? pageSize, CancellationToken ct) =>
        {
            service.RequirePlatformAdmin();
            var rows = await service.ListAsync<TenantMembership>(x => x.TenantId == tenantId && (status == null || x.Status == status) && (string.IsNullOrWhiteSpace(search) || x.UserId.Contains(search)), page ?? 1, pageSize ?? 50, ct);
            return Results.Ok(ApiResponse<IReadOnlyList<MembershipResponse>>.Success(rows.Select(ToResponse).ToArray(), Trace()));
        });
        // Tenant admins can add members only to their own tenant; platform admins can also use this route.
        api.MapPost("/tenants/{tenantId:int}/users", async (int tenantId, CreateTenantUserRequest request, IPlatformAuthService auth, CancellationToken ct) =>
        {
            var created = await auth.CreateTenantUserAsync(tenantId, request, ct);
            return Results.Created($"/api/v1/tenants/{tenantId}/memberships", ApiResponse<TenantUserCreatedResponse>.Success(created, Trace()));
        });
        api.MapPost("/tenants/{tenantId:int}/memberships", async (int tenantId, CreateMembershipRequest request, IPlatformService service, CancellationToken ct) =>
        {
            var row = await service.AddMembershipAsync(tenantId, request.UserId, ct);
            return Results.Created($"/api/v1/tenants/{tenantId}/memberships/{row.Id}", ApiResponse<MembershipResponse>.Success(ToResponse(row), Trace()));
        });
        api.MapGet("/tenants/{tenantId:int}/memberships/{membershipId:int}", async (int tenantId, int membershipId, IPlatformService service, CancellationToken ct) =>
        {
            service.RequirePlatformAdmin();
            var row = await service.FindAsync<TenantMembership>(x => x.Id == membershipId && x.TenantId == tenantId, ct) ?? throw NotFound("MEMBERSHIP_NOT_FOUND", "Tenant membership was not found.");
            var actions = await service.ListAsync<TenantUserAction>(x => x.TenantMembershipId == membershipId, 1, 100, ct);
            return Results.Ok(ApiResponse<MembershipDetailResponse>.Success(new(ToResponse(row), actions.Select(ToResponse).ToArray()), Trace()));
        });
        api.MapPatch("/tenants/{tenantId:int}/memberships/{membershipId:int}", async (int tenantId, int membershipId, UpdateMembershipRequest request, IPlatformService service, CancellationToken ct) =>
        {
            await service.UpdateMembershipAsync(tenantId, membershipId, request.Status, ct);
            return Results.NoContent();
        });

        api.MapGet("/tenants/{tenantId:int}/memberships/{membershipId:int}/actions", async (int tenantId, int membershipId, IPlatformService service, int? productId, bool? isActive, int? page, int? pageSize, CancellationToken ct) =>
        {
            service.RequirePlatformAdmin();
            var membership = await service.FindAsync<TenantMembership>(x => x.Id == membershipId && x.TenantId == tenantId, ct) ?? throw NotFound("MEMBERSHIP_NOT_FOUND", "Tenant membership was not found.");
            var rows = await service.ListAsync<TenantUserAction>(x => x.TenantMembershipId == membership.Id && (isActive == null || x.IsActive == isActive) && (productId == null || x.Action.Feature.Module.ProductId == productId), page ?? 1, pageSize ?? 50, ct);
            return Results.Ok(ApiResponse<IReadOnlyList<UserActionResponse>>.Success(rows.Select(ToResponse).ToArray(), Trace()));
        });
        api.MapPut("/tenants/{tenantId:int}/memberships/{membershipId:int}/actions/{actionId:int}", async (int tenantId, int membershipId, int actionId, IPlatformService service, CancellationToken ct) =>
        {
            var row = await service.AssignActionAsync(tenantId, membershipId, actionId, ct);
            return Results.Ok(ApiResponse<UserActionResponse>.Success(ToResponse(row), Trace()));
        });
        api.MapDelete("/tenants/{tenantId:int}/memberships/{membershipId:int}/actions/{actionId:int}", async (int tenantId, int membershipId, int actionId, IPlatformService service, CancellationToken ct) =>
        {
            await service.RevokeActionAsync(tenantId, membershipId, actionId, ct);
            return Results.NoContent();
        });
        // Return only currently usable grants, after rechecking membership, catalog status, and product subscription.
        api.MapGet("/tenants/{tenantId:int}/me/actions", async (int tenantId, int? productId, int? page, int? pageSize, IPlatformService service, CancellationToken ct) =>
        {
            var rows = await service.ListMyActionsAsync(tenantId, productId, ct);
            var currentPage = page ?? 1;
            var currentSize = pageSize ?? 50;
            if (currentPage < 1 || currentSize is < 1 or > 100) throw new ApiException(400, "INVALID_PAGINATION", "Page must be positive and pageSize must be between 1 and 100.");
            return Results.Ok(ApiResponse<IReadOnlyList<UserActionResponse>>.Success(rows.Skip((currentPage - 1) * currentSize).Take(currentSize).Select(ToResponse).ToArray(), Trace()));
        });
        api.MapPost("/tenants/{tenantId:int}/authorization/check", async (int tenantId, AccessCheckRequest request, IPlatformService service, CancellationToken ct) =>
        {
            var result = await service.CheckAccessAsync(tenantId, request.ProductKey, request.ModuleKey, request.FeatureKey, request.ActionKey, ct);
            return Results.Ok(ApiResponse<AuthorizationDecision>.Success(result, Trace()));
        });

        return endpoints;
    }

    private static async Task<ProductModule> RequireModule(int productId, int moduleId, IPlatformService service, CancellationToken ct) =>
        await service.FindAsync<ProductModule>(x => x.Id == moduleId && x.ProductId == productId, ct) ?? throw NotFound("MODULE_NOT_FOUND", "Product module was not found.");

    private static async Task<IReadOnlyList<SubscriptionResponse>> GetActiveSubscriptions(IPlatformService service, int tenantId, CancellationToken ct)
    {
        var rows = await service.ListAsync<TenantProductSubscription>(
            x => x.TenantId == tenantId && x.Status == TenantProductSubscriptionStatus.Active && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > DateTimeOffset.UtcNow),
            1, 100, ct);
        return rows.Select(ToResponse).ToArray();
    }

    private static async Task<ProductFeature> RequireFeature(int productId, int moduleId, int featureId, IPlatformService service, CancellationToken ct)
    {
        await RequireModule(productId, moduleId, service, ct);
        return await service.FindAsync<ProductFeature>(x => x.Id == featureId && x.ProductModuleId == moduleId, ct) ?? throw NotFound("FEATURE_NOT_FOUND", "Product feature was not found.");
    }

    private static ApiException NotFound(string code, string message) => new(404, code, message);
    private static string Required(string? value, string field) => string.IsNullOrWhiteSpace(value)
        ? throw new ApiException(400, "REQUIRED_FIELD", $"{field} is required.")
        : value.Trim();
    private static string Trace() => System.Diagnostics.Activity.Current?.Id ?? string.Empty;
    private static ProductResponse ToResponse(Product x) => new(x.Id, x.Key, x.Name, x.Status);
    private static ModuleResponse ToResponse(ProductModule x) => new(x.Id, x.ProductId, x.Key, x.Name, x.Status, x.SortOrder);
    private static FeatureResponse ToResponse(ProductFeature x) => new(x.Id, x.ProductModuleId, x.Key, x.Name, x.Status, x.SortOrder);
    private static ActionResponse ToResponse(ProductAction x) => new(x.Id, x.ProductFeatureId, x.Key, x.Name, x.Status);
    private static TenantResponse ToResponse(Tenant x) => new(x.Id, x.Key, x.Name, x.Status);
    private static SubscriptionResponse ToResponse(TenantProductSubscription x) => new(x.Id, x.TenantId, x.ProductId, x.Status, x.SubscribedAtUtc, x.ExpiresAtUtc);
    private static MembershipResponse ToResponse(TenantMembership x) => new(x.Id, x.TenantId, x.UserId, x.Status, x.Role, x.CreatedAtUtc);
    private static UserActionResponse ToResponse(TenantUserAction x) => new(x.Id, x.TenantMembershipId, x.ProductActionId, x.IsActive, x.AssignedAtUtc);
}
