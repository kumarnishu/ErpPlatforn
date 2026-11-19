namespace Todos.Api;

public sealed class TodoItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool IsDeleted { get; set; }
}

public sealed record CreateTodoRequest(string Title);
public sealed record UpdateTodoRequest(string Title, bool IsCompleted);
