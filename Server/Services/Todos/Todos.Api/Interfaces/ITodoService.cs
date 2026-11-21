using Todos.Api.Models;

namespace Todos.Api.Interfaces;

public interface ITodoService
{
    Task<IReadOnlyList<TodoItem>> GetForTenantAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<TodoItem?> GetAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task<TodoItem> CreateAsync(Guid tenantId, string title, CancellationToken cancellationToken);
    Task<TodoItem?> UpdateAsync(Guid tenantId, Guid id, string title, bool isCompleted, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
}
