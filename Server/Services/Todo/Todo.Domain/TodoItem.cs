namespace Todo.Domain;

/// <summary>A task owned by one authenticated user inside a tenant's Todo database.</summary>
public sealed class TodoItem
{
    private TodoItem() { }

    private TodoItem(Guid id, string tenantId, string ownerUserId, string title, DateTimeOffset createdAtUtc)
    {
        Id = id;
        TenantId = tenantId;
        OwnerUserId = ownerUserId;
        Title = title;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string OwnerUserId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public bool IsCompleted { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public static TodoItem Create(string tenantId, string ownerUserId, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new TodoItem(Guid.NewGuid(), tenantId, ownerUserId, title.Trim(), DateTimeOffset.UtcNow);
    }

    public void Rename(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title.Trim();
    }

    public void MarkCompleted(DateTimeOffset completedAtUtc)
    {
        IsCompleted = true;
        CompletedAtUtc = completedAtUtc;
    }

    public void Reopen()
    {
        IsCompleted = false;
        CompletedAtUtc = null;
    }
}
