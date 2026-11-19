using Platform.Api.Interfaces;

namespace Platform.Api.Models;

/// <summary>A user account belonging to one tenant.</summary>
public sealed class PlatformUser : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>Canonical lower-case email used for tenant-scoped sign-in lookup.</summary>
    public string NormalizedEmail { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Google Cloud Storage object name for the user's profile photo.</summary>
    public string? ProfilePhotoObjectName { get; set; }

    /// <summary>Store a password hash produced by a password hashing library; never store a raw password.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "User";

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    /// <summary>Invalidates previously issued tokens when changed.</summary>
    public bool TokenVersion { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Tenant? Tenant { get; set; }
}
