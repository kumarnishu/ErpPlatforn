namespace Platform.Domain;

public enum TenantStatus
{
    Active,
    Suspended
}

public enum TenantMembershipRole
{
    TenantAdmin,
    TenantUser
}

public sealed class Tenant
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<TenantMembership> Memberships { get; set; } = new List<TenantMembership>();
}

public sealed class User
{
    public int Id { get; set; }
    public string UserId { get; set; } = $"usr_{Guid.NewGuid():N}";
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<TenantMembership> Memberships { get; set; } = new List<TenantMembership>();
}

public sealed class TenantMembership
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public TenantMembershipRole Role { get; set; } = TenantMembershipRole.TenantUser;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Tenant Tenant { get; set; } = null!;
    public User User { get; set; } = null!;
}
