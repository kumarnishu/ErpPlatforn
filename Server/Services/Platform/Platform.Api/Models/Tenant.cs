using Platform.Api.Interfaces;

namespace Platform.Api.Models;

/// <summary>A customer organization in the ERP platform.</summary>
public sealed class Tenant : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    /// <summary>Short tenant identifier carried in authentication claims.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Google Cloud Storage object name for the tenant logo.</summary>
    public string? LogoObjectName { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public TenantSubscription? Subscription { get; set; }
}
