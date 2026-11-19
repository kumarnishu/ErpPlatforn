using Platform.Api.Interfaces;

namespace Platform.Api.Models;

/// <summary>A user's login session and its refresh-token state.</summary>
public sealed class UserSession : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public Guid TenantId { get; set; }

    /// <summary>Store a SHA-256 hash of the refresh token, never the raw token.</summary>
    public string RefreshTokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? LastUsedAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public PlatformUser? User { get; set; }

    public Tenant? Tenant { get; set; }
}
