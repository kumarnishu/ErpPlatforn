using Todo.Domain;

namespace Todo.Application;

/// <summary>Persistence boundary; implementations must scope all operations to tenant and owner.</summary>
public interface ITodoRepository
{
    Task<IReadOnlyList<TodoItem>> ListForUserAsync(
        string tenantId,
        string ownerUserId,
        CancellationToken cancellationToken = default);

    Task<TodoItem?> GetForUserAsync(
        string tenantId,
        string ownerUserId,
        Guid todoId,
        CancellationToken cancellationToken = default);

    Task AddAsync(TodoItem item, CancellationToken cancellationToken = default);
    Task DeleteAsync(TodoItem item, CancellationToken cancellationToken = default);
}
