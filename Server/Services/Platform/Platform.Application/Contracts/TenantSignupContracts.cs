namespace Platform.Application.Contracts;

public sealed record TenantSignupRequest(
    string TenantName,
    string TenantKey,
    string AdminName,
    string AdminEmail,
    string Password);

public sealed record TenantSignupResult(
    int TenantId,
    string TenantKey,
    string TenantName,
    string AdminUserId,
    string AdminEmail,
    string AdminName,
    string Role,
    DateTimeOffset CreatedAtUtc);
