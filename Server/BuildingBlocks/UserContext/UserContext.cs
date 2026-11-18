namespace BuildingBlocks.UserContext;

/// <summary>Login data used to issue a JWT.</summary>
public sealed record UserContext
{
    public Guid UserId { get; }
    public Guid TenantId { get; }
    public string TenantKey { get; }
    public string Email { get; }
    public bool IsUserActive { get; }
    public bool IsTenantActive { get; }
    public bool TokenVersion { get; }
    public Guid SessionId { get; }
    public string Role { get; }
    public DateTimeOffset SubscriptionExpiry { get; }

    public UserContext(
        Guid userId,
        Guid tenantId,
        string tenantKey,
        string email,
        bool isUserActive,
        bool isTenantActive,
        bool tokenVersion,
        Guid sessionId,
        string role,
        DateTimeOffset subscriptionExpiry)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId must not be an empty UUID.", nameof(userId));
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId must not be an empty UUID.", nameof(tenantId));
        if (sessionId == Guid.Empty) throw new ArgumentException("SessionId must not be an empty UUID.", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(tenantKey) || tenantKey.Length is < 2 or > 4)
            throw new ArgumentException("TenantKey must contain between 2 and 4 characters.", nameof(tenantKey));
        ArgumentNullException.ThrowIfNull(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        UserId = userId;
        TenantId = tenantId;
        TenantKey = tenantKey;
        Email = email;
        IsUserActive = isUserActive;
        IsTenantActive = isTenantActive;
        TokenVersion = tokenVersion;
        SessionId = sessionId;
        Role = role;
        SubscriptionExpiry = subscriptionExpiry;
    }
}
