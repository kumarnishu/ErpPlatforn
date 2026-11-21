using Platform.Api.Interfaces;

namespace Platform.Api.Models;

public enum TenantSubscriptionStatus
{
    Trial = 0,
    Active = 1,
    PastDue = 2,
    Suspended = 3,
    Cancelled = 4,
    Expired = 5
}

public enum SubscriptionBillingInterval
{
    Monthly = 0,
    Yearly = 1
}

/// <summary>A subscription period and plan assignment for a tenant.</summary>
public sealed class TenantSubscription : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    /// <summary>Stable identifier for the selected plan, such as "standard".</summary>
    public string PlanCode { get; set; } = string.Empty;

    public TenantSubscriptionStatus Status { get; set; } = TenantSubscriptionStatus.Trial;

    public SubscriptionBillingInterval BillingInterval { get; set; } = SubscriptionBillingInterval.Monthly;

    public DateTimeOffset StartsAtUtc { get; set; }

    public DateTimeOffset CurrentPeriodEndsAtUtc { get; set; }

    public DateTimeOffset? CancelledAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Tenant? Tenant { get; set; }

    public ICollection<PlatformFeature> PlatformFeatures { get; set; } = new List<PlatformFeature>();
}
