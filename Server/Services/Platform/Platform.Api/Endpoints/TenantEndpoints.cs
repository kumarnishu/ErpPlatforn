using System.Net.Mail;
using BuildingBlocks.ApiResponses;
using BuildingBlocks.UserContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.Api.Data;
using Platform.Api.Models;

namespace Platform.Api.Endpoints;

public static class TenantEndpoints
{
    private const string TenantAdminRole = "TenantAdmin";
    private const string TenantUserRole = "TenantUser";

    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/tenants");

        // Tenant registration creates the first administrator for that tenant.
        group.MapPost("/", CreateTenantAsync);

        // Authenticated tenant administrators can create ordinary users only in their own tenant.
        group.MapPost("/users", CreateTenantUserAsync).RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> CreateTenantAsync(
        CreateTenantRequest request,
        PlatformDbContext db,
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
        PlatformDbContext db,
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

public sealed record CreateTenantUserRequest(string? DisplayName, string? Email, string? Password);
public sealed record TenantAdminCreatedResponse(Guid Id, string Email, string DisplayName, string Role);
public sealed record TenantCreatedResponse(
    Guid Id,
    string Name,
    string Key,
    TenantAdminCreatedResponse Admin,
    DateTimeOffset TrialExpiresAtUtc);
public sealed record TenantUserCreatedResponse(Guid Id, Guid TenantId, string Email, string DisplayName, string Role);
