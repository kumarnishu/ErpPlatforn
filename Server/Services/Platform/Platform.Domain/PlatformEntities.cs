namespace Platform.Domain;

// The platform catalog defines product permissions; tenant memberships receive explicit action assignments.
public enum TenantStatus { Active, Suspended }
public enum CatalogStatus { Active, Inactive }
public enum TenantProductSubscriptionStatus { Active, Suspended, Cancelled, Expired }
public enum TenantMembershipStatus { Active, Revoked }
public enum TenantMembershipRole { TenantAdmin, TenantUser }

/// <summary>A local login account used by tenant members.</summary>
public class PlatformUser
{
    public int Id { get; set; }
    public string UserId { get; set; } = $"usr_{Guid.NewGuid():N}";
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>A customer organization on the platform.</summary>
public class Tenant
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();
    public ICollection<TenantProductSubscription> ProductSubscriptions { get; set; } = new List<TenantProductSubscription>();
    public ICollection<TenantMembership> Members { get; set; } = new List<TenantMembership>();
}

/// <summary>A product offered by the platform, such as ERP, Checklist, Data Studio, or HRMS.</summary>
public class Product
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
    public ICollection<ProductModule> Modules { get; set; } = new List<ProductModule>();
    public ICollection<TenantProductSubscription> TenantSubscriptions { get; set; } = new List<TenantProductSubscription>();
}

/// <summary>A module within a product.</summary>
public class ProductModule
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
    public int SortOrder { get; set; }
    public Product Product { get; set; } = null!;
    public ICollection<ProductFeature> Features { get; set; } = new List<ProductFeature>();
}

/// <summary>A feature within a product module.</summary>
public class ProductFeature
{
    public int Id { get; set; }
    public int ProductModuleId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
    public int SortOrder { get; set; }
    public ProductModule Module { get; set; } = null!;
    public ICollection<ProductAction> Actions { get; set; } = new List<ProductAction>();
}

/// <summary>A permission/action available on a feature, such as view, create, edit, export, or delete.</summary>
public class ProductAction
{
    public int Id { get; set; }
    public int ProductFeatureId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
    public ProductFeature Feature { get; set; } = null!;
    public ICollection<TenantUserAction> UserAssignments { get; set; } = new List<TenantUserAction>();
}

/// <summary>A tenant's subscription to one product. Each product is subscribed to independently.</summary>
public class TenantProductSubscription
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ProductId { get; set; }
    public TenantProductSubscriptionStatus Status { get; set; } = TenantProductSubscriptionStatus.Active;
    public DateTimeOffset SubscribedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

/// <summary>A user belonging to a tenant. UserId is the stable subject from the authentication provider.</summary>
public class TenantMembership
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public TenantMembershipStatus Status { get; set; } = TenantMembershipStatus.Active;
    public TenantMembershipRole Role { get; set; } = TenantMembershipRole.TenantUser;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Tenant Tenant { get; set; } = null!;
    public ICollection<TenantUserAction> ActionAssignments { get; set; } = new List<TenantUserAction>();
}

/// <summary>An action explicitly granted to one tenant member.</summary>
public class TenantUserAction
{
    public int Id { get; set; }
    public int TenantMembershipId { get; set; }
    public int ProductActionId { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool IsActive { get; set; } = true;
    public TenantMembership Membership { get; set; } = null!;
    public ProductAction Action { get; set; } = null!;
}
