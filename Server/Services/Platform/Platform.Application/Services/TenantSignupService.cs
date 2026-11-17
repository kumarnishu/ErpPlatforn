using BuildingBlocks.Http;
using Platform.Application.Abstractions;
using Platform.Application.Contracts;
using Platform.Domain;

namespace Platform.Application.Services;

public sealed class TenantSignupService(
    ITenantSignupRepository repository,
    IPasswordHashService passwords) : ITenantSignupService
{
    public async Task<TenantSignupResult> SignUpAsync(TenantSignupRequest request, CancellationToken cancellationToken)
    {
        Validate(request);

        var tenantKey = NormalizeKey(request.TenantKey);
        var email = request.AdminEmail.Trim();
        var normalizedEmail = email.ToUpperInvariant();

        if (await repository.TenantKeyExistsAsync(tenantKey, cancellationToken))
            throw new ApiException(409, "TENANT_KEY_EXISTS", "That tenant key is already in use.");
        if (await repository.EmailExistsAsync(normalizedEmail, cancellationToken))
            throw new ApiException(409, "EMAIL_EXISTS", "An account with that email already exists.");

        var tenant = new Tenant { Key = tenantKey, Name = request.TenantName.Trim() };
        var user = new User
        {
            Email = email,
            NormalizedEmail = normalizedEmail,
            DisplayName = request.AdminName.Trim(),
            PasswordHash = passwords.Hash(request.Password)
        };
        var membership = new TenantMembership
        {
            Tenant = tenant,
            User = user,
            UserId = user.UserId,
            Role = TenantMembershipRole.TenantAdmin
        };
        tenant.Memberships.Add(membership);
        user.Memberships.Add(membership);

        try
        {
            await repository.AddTenantWithAdminAsync(tenant, user, membership, cancellationToken);
        }
        catch (TenantSignupConflictException exception) when (exception.Conflict == "tenant-key")
        {
            throw new ApiException(409, "TENANT_KEY_EXISTS", "That tenant key is already in use.");
        }
        catch (TenantSignupConflictException)
        {
            throw new ApiException(409, "EMAIL_EXISTS", "An account with that email already exists.");
        }

        return new TenantSignupResult(
            tenant.Id,
            tenant.Key,
            tenant.Name,
            user.UserId,
            user.Email,
            user.DisplayName,
            membership.Role.ToString(),
            tenant.CreatedAtUtc);
    }

    private static void Validate(TenantSignupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TenantName) || request.TenantName.Trim().Length > 200)
            throw new ApiException(400, "INVALID_TENANT_NAME", "Tenant name is required and must be at most 200 characters.");
        if (string.IsNullOrWhiteSpace(request.TenantKey) || request.TenantKey.Trim().Length > 100)
            throw new ApiException(400, "INVALID_TENANT_KEY", "Tenant key is required and must be at most 100 characters.");
        if (string.IsNullOrWhiteSpace(request.AdminName) || request.AdminName.Trim().Length > 200)
            throw new ApiException(400, "INVALID_ADMIN_NAME", "Admin name is required and must be at most 200 characters.");
        if (string.IsNullOrWhiteSpace(request.AdminEmail) || request.AdminEmail.Trim().Length > 320 || !IsEmail(request.AdminEmail.Trim()))
            throw new ApiException(400, "INVALID_EMAIL", "A valid admin email address is required.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 12 || request.Password.Length > 256)
            throw new ApiException(400, "INVALID_PASSWORD", "Password must contain between 12 and 256 characters.");
    }

    private static string NormalizeKey(string key) => key.Trim().ToLowerInvariant();

    private static bool IsEmail(string email)
    {
        try { return new System.Net.Mail.MailAddress(email).Address == email; }
        catch (FormatException) { return false; }
    }
}
