namespace Platform.Api.Models;

/// <summary>A platform service enabled for a tenant subscription.</summary>
public sealed class PlatformFeature
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Stable identifier such as "erp", "hrms", "checklists", or "todos".</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    /// <summary>Whether this feature's tenant database migrations have been applied.</summary>
    public bool IsDbCreated { get; set; }

    public Guid TenantSubscriptionId { get; set; }

    public TenantSubscription? TenantSubscription { get; set; }
}
