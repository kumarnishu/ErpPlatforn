using System.Net.Mail;
using BuildingBlocks.ApiResponses;
using BuildingBlocks.TenantMigrations;
using BuildingBlocks.UserContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.Api.Data;
using Platform.Api.Models;
using Platform.Api.Services;

namespace Platform.Api.Endpoints;

public static class TenantEndpoints
{
    private const string TenantAdminRole = "TenantAdmin";
    private const string TenantUserRole = "TenantUser";
    private static readonly PlatformFeatureCatalogItem[] PlatformFeatureCatalog =
    [
        new("erp", "ERP", 1000m),
        new("hrms", "HRMS", 1000m),
        new("checklists", "Checklists", 1000m),
        new("todos", "Todos", 1000m)
    ];

    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/tenants");

        // Register a tenant and its first administrator.
        group.MapPost("/", CreateTenantAsync);

        // List available platform features and their prices.
        group.MapGet("/features", GetPlatformFeatures);

        // Create a user in the administrator's tenant.
        group.MapPost("/users", CreateTenantUserAsync).RequireAuthorization();

        // Get the signed-in user's profile and tenant.
        group.MapGet("/me", GetCurrentUserProfileAsync).RequireAuthorization();

        // Replace the tenant's enabled platform features.
        group.MapPut("/me/features", UpdateFeaturesAsync).RequireAuthorization();

        return endpoints;
    }

    private static IResult GetPlatformFeatures(HttpContext context) =>
        Results.Ok(ApiResponse<PlatformFeaturesCatalogResponse>.Success(
            new PlatformFeaturesCatalogResponse(PlatformFeatureCatalog), context.TraceIdentifier));

    private static async Task<IResult> UpdateFeaturesAsync(
        UpdatePlatformFeaturesRequest request,
        IUserContextAccessor accessor,
        ITenantDatabaseMigrationService migrations,
        [FromServices] PlatformDbContext db,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var current = accessor.Current;
        if (current is null || !string.Equals(current.Role, TenantAdminRole, StringComparison.Ordinal))
            return Forbidden(context, "TENANT_ADMIN_REQUIRED", "Only a tenant administrator can update platform features.");

        var allowed = PlatformFeatureCatalog.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var codes = request.FeatureCodes?.Select(x => x?.Trim().ToLowerInvariant()).ToArray();
        if (codes is null || codes.Any(x => string.IsNullOrWhiteSpace(x) || !allowed.ContainsKey(x)) ||
            codes.Distinct(StringComparer.Ordinal).Count() != codes.Length)
            return BadRequest(context, "INVALID_PLATFORM_FEATURES", "Provide distinct feature codes from: erp, hrms, checklists, todos.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Serialize feature updates for this tenant so another request cannot remove or
        // modify a feature row between loading it and saving the updated migration state.
        var lockedSubscriptionId = await db.Database
            .SqlQuery<Guid>($"SELECT id AS \"Value\" FROM tenant_subscriptions WHERE tenant_id = {current.TenantId} AND NOT is_deleted FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (lockedSubscriptionId == Guid.Empty)
            return Results.NotFound(ApiResponse<object>.Failure(
                new ApiError("SUBSCRIPTION_NOT_FOUND", "The tenant subscription could not be found."), context.TraceIdentifier));

        var subscription = await db.TenantSubscriptions
            .Include(x => x.PlatformFeatures)
            .SingleOrDefaultAsync(x => x.Id == lockedSubscriptionId, cancellationToken);
        if (subscription is null)
            return Results.NotFound(ApiResponse<object>.Failure(
                new ApiError("SUBSCRIPTION_NOT_FOUND", "The tenant subscription could not be found."), context.TraceIdentifier));

        var selected = codes.Select(x => x!).ToHashSet(StringComparer.Ordinal);
        var featuresToInitialize = subscription.PlatformFeatures
            .Where(x => selected.Contains(x.Code) && !x.IsDbCreated)
            .Select(x => x.Code)
            .Concat(selected.Where(code => subscription.PlatformFeatures.All(x => !string.Equals(x.Code, code, StringComparison.Ordinal))))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // Initialize selected feature schemas only once, before saving the selection.
        var initializedFeatures = await migrations.ApplySelectedAsync(current.TenantKey, featuresToInitialize, cancellationToken);
        db.PlatformFeatures.RemoveRange(subscription.PlatformFeatures.Where(x => !selected.Contains(x.Code)));
        var existing = subscription.PlatformFeatures.Select(x => x.Code).ToHashSet(StringComparer.Ordinal);
        foreach (var code in selected.Where(x => !existing.Contains(x)))
        {
            var feature = new PlatformFeature
            {
                Code = code,
                Name = allowed[code].Name,
                Price = allowed[code].Price,
                TenantSubscriptionId = subscription.Id,
                IsDbCreated = initializedFeatures.Contains(code)
            };
            subscription.PlatformFeatures.Add(feature);
        }
        foreach (var feature in subscription.PlatformFeatures.Where(x => selected.Contains(x.Code) && initializedFeatures.Contains(x.Code)))
            feature.IsDbCreated = true;

        subscription.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Results.Ok(ApiResponse<PlatformFeaturesResponse>.Success(
            new PlatformFeaturesResponse(selected.OrderBy(x => x).ToArray()), context.TraceIdentifier));
    }

    private static async Task<IResult> CreateTenantAsync(
        CreateTenantRequest request,
        [FromServices] PlatformDbContext db,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var tenantName = request.TenantName?.Trim();
        var tenantKey = request.TenantKey?.Trim().ToUpperInvariant();
        var adminName = request.AdminDisplayName?.Trim();
        var adminEmail = NormalizeEmail(request.AdminEmail);

        if (string.IsNullOrWhiteSpace(tenantName) || tenantName.Length > 200 ||
            !IsValidTenantKey(tenantKey) || string.IsNullOrWhiteSpace(adminName) || adminName.Length > 200 ||
            !IsValidEmail(adminEmail) || !IsValidPassword(request.AdminPassword))
            return BadRequest(context, "INVALID_TENANT_REQUEST",
                "Tenant name, a 2-4 character alphanumeric tenant key, admin name, valid admin email, and a password of at least 12 characters are required.");

        if (await db.Tenants.AnyAsync(x => x.Key == tenantKey, cancellationToken))
            return Conflict(context, "TENANT_KEY_IN_USE", "That tenant key is already in use.");

        var now = DateTimeOffset.UtcNow;
        var tenant = new Tenant { Name = tenantName, Key = tenantKey! };
        var admin = new PlatformUser
        {
            Tenant = tenant,
            Email = adminEmail!,
            NormalizedEmail = adminEmail!,
            DisplayName = adminName!,
            Role = TenantAdminRole,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        admin.PasswordHash = new PasswordHasher<PlatformUser>().HashPassword(admin, request.AdminPassword!);

        // Give new tenants a 30-day trial so the initial administrator can sign in immediately.
        var subscription = new TenantSubscription
        {
            Tenant = tenant,
            PlanCode = "trial",
            Status = TenantSubscriptionStatus.Trial,
            BillingInterval = SubscriptionBillingInterval.Monthly,
            StartsAtUtc = now,
            CurrentPeriodEndsAtUtc = now.AddDays(30),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.Tenants.Add(tenant);
        db.Users.Add(admin);
        db.TenantSubscriptions.Add(subscription);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(context, "TENANT_OR_EMAIL_IN_USE", "The tenant key or admin email is already in use.");
        }

        return Results.Created($"/api/v1/tenants/{tenant.Id}",
            ApiResponse<TenantCreatedResponse>.Success(
                new TenantCreatedResponse(tenant.Id, tenant.Name, tenant.Key,
                    new TenantAdminCreatedResponse(admin.Id, admin.Email, admin.DisplayName, admin.Role),
                    subscription.CurrentPeriodEndsAtUtc),
                context.TraceIdentifier));
    }

    private static async Task<IResult> CreateTenantUserAsync(
        CreateTenantUserRequest request,
        IUserContextAccessor accessor,
        [FromServices] PlatformDbContext db,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var current = accessor.Current;
        if (current is null || !string.Equals(current.Role, TenantAdminRole, StringComparison.Ordinal))
            return Forbidden(context, "TENANT_ADMIN_REQUIRED", "Only a tenant administrator can create tenant users.");

        var name = request.DisplayName?.Trim();
        var email = NormalizeEmail(request.Email);
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200 || !IsValidEmail(email) || !IsValidPassword(request.Password))
            return BadRequest(context, "INVALID_USER_REQUEST",
                "A display name, valid email, and a password of at least 12 characters are required.");

        var tenant = await db.Tenants.SingleOrDefaultAsync(x => x.Id == current.TenantId, cancellationToken);
        if (tenant is null || !tenant.IsActive || !current.IsTenantActive)
            return Forbidden(context, "INACTIVE_TENANT", "The tenant is inactive.");

        if (await db.Users.AnyAsync(x => x.TenantId == tenant.Id && x.NormalizedEmail == email, cancellationToken))
            return Conflict(context, "EMAIL_IN_USE", "That email is already in use in this tenant.");

        var now = DateTimeOffset.UtcNow;
        var user = new PlatformUser
        {
            TenantId = tenant.Id,
            Email = email!,
            NormalizedEmail = email!,
            DisplayName = name!,
            Role = TenantUserRole,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        user.PasswordHash = new PasswordHasher<PlatformUser>().HashPassword(user, request.Password!);

        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(context, "EMAIL_IN_USE", "That email is already in use in this tenant.");
        }

        return Results.Created($"/api/v1/tenants/users/{user.Id}",
            ApiResponse<TenantUserCreatedResponse>.Success(
                new TenantUserCreatedResponse(user.Id, user.TenantId, user.Email, user.DisplayName, user.Role),
                context.TraceIdentifier));
    }

    private static async Task<IResult> GetCurrentUserProfileAsync(
        IUserContextAccessor accessor,
        [FromServices] PlatformDbContext db,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var current = accessor.Current;
        if (current is null)
            return Results.Json(
                ApiResponse<object>.Failure(
                    new ApiError("INVALID_SESSION", "The authenticated user context is unavailable."),
                    context.TraceIdentifier),
                statusCode: StatusCodes.Status401Unauthorized);

        var user = await db.Users
            .AsNoTracking()
            .Include(x => x.Tenant)
            .SingleOrDefaultAsync(
                x => x.Id == current.UserId && x.TenantId == current.TenantId,
                cancellationToken);

        if (user?.Tenant is not { } tenant)
            return Results.NotFound(
                ApiResponse<object>.Failure(
                    new ApiError("USER_PROFILE_NOT_FOUND", "The user profile or tenant could not be found."),
                    context.TraceIdentifier));

        var profile = new TenantUserProfileResponse(
            new UserProfileResponse(
                user.Id,
                user.Email,
                user.DisplayName,
                user.ProfilePhotoObjectName,
                user.Role,
                user.IsActive,
                user.CreatedAtUtc,
                user.UpdatedAtUtc),
            new TenantInfoResponse(
                tenant.Id,
                tenant.Name,
                tenant.Key,
                tenant.LogoObjectName,
                tenant.IsActive,
                tenant.CreatedAtUtc,
                tenant.UpdatedAtUtc));

        return Results.Ok(ApiResponse<TenantUserProfileResponse>.Success(profile, context.TraceIdentifier));
    }

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 320) return false;
        try { return new MailAddress(email).Address == email; }
        catch (FormatException) { return false; }
    }

    private static bool IsValidPassword(string? password) =>
        !string.IsNullOrWhiteSpace(password) && password.Length is >= 12 and <= 128;

    private static bool IsValidTenantKey(string? key) =>
        key is { Length: >= 2 and <= 4 } && key.All(char.IsAsciiLetterOrDigit);

    private static IResult BadRequest(HttpContext context, string code, string message) => Results.BadRequest(
        ApiResponse<object>.Failure(new ApiError(code, message), context.TraceIdentifier));

    private static IResult Conflict(HttpContext context, string code, string message) => Results.Conflict(
        ApiResponse<object>.Failure(new ApiError(code, message), context.TraceIdentifier));

    private static IResult Forbidden(HttpContext context, string code, string message) => Results.Json(
        ApiResponse<object>.Failure(new ApiError(code, message), context.TraceIdentifier),
        statusCode: StatusCodes.Status403Forbidden);
}

public sealed record CreateTenantRequest(
    string? TenantName,
    string? TenantKey,
    string? AdminDisplayName,
    string? AdminEmail,
    string? AdminPassword);

public sealed record UpdatePlatformFeaturesRequest(string?[]? FeatureCodes);
public sealed record PlatformFeaturesResponse(string[] FeatureCodes);
public sealed record PlatformFeaturesCatalogResponse(PlatformFeatureCatalogItem[] Features);
public sealed record PlatformFeatureCatalogItem(string Code, string Name, decimal Price);

public sealed record CreateTenantUserRequest(string? DisplayName, string? Email, string? Password);
public sealed record TenantUserProfileResponse(UserProfileResponse User, TenantInfoResponse Tenant);
public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string? ProfilePhotoObjectName,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
public sealed record TenantInfoResponse(
    Guid Id,
    string Name,
    string Key,
    string? LogoObjectName,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
public sealed record TenantAdminCreatedResponse(Guid Id, string Email, string DisplayName, string Role);
public sealed record TenantCreatedResponse(
    Guid Id,
    string Name,
    string Key,
    TenantAdminCreatedResponse Admin,
    DateTimeOffset TrialExpiresAtUtc);
public sealed record TenantUserCreatedResponse(Guid Id, Guid TenantId, string Email, string DisplayName, string Role);
