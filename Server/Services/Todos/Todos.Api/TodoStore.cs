using System.Collections.Concurrent;

namespace Todos.Api;

public sealed record TodoItem(
    Guid Id,
    Guid TenantId,
    string Title,
    bool IsCompleted,
    DateTimeOffset CreatedAtUtc,
    bool IsDeleted = false);

public sealed record CreateTodoRequest(string Title);
public sealed record UpdateTodoRequest(string Title, bool IsCompleted);

/// <summary>Stores todo items in memory for local development.</summary>
public sealed class TodoStore
{
    private readonly ConcurrentDictionary<Guid, TodoItem> _items = new();

    public IEnumerable<TodoItem> ForTenant(Guid tenantId) =>
        _items.Values.Where(item => item.TenantId == tenantId && !item.IsDeleted).OrderByDescending(item => item.CreatedAtUtc);

    public TodoItem? Find(Guid tenantId, Guid id) =>
        _items.TryGetValue(id, out var item) && item.TenantId == tenantId && !item.IsDeleted ? item : null;

    public TodoItem Add(Guid tenantId, string title)
    {
        var item = new TodoItem(Guid.NewGuid(), tenantId, title.Trim(), false, DateTimeOffset.UtcNow);
        _items[item.Id] = item;
        return item;
    }

    public TodoItem? Update(Guid tenantId, Guid id, string title, bool isCompleted)
    {
        while (_items.TryGetValue(id, out var current))
        {
            if (current.TenantId != tenantId) return null;
            var updated = current with { Title = title.Trim(), IsCompleted = isCompleted };
            if (_items.TryUpdate(id, updated, current)) return updated;
        }

        return null;
    }

    public bool Remove(Guid tenantId, Guid id) =>
        _items.TryGetValue(id, out var item)
        && item.TenantId == tenantId
        && !item.IsDeleted
        && _items.TryUpdate(id, item with { IsDeleted = true }, item);
}
