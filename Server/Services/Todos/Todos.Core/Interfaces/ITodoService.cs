using Todos.Core.Models;

namespace Todos.Core.Interfaces;

/// <summary>Performs todo operations for a tenant.</summary>
public interface ITodoService
{
    Task<IReadOnlyList<TodoItem>> GetForTenantAsync(string tenantKey, Guid tenantId, CancellationToken cancellationToken);
    Task<TodoItem?> GetAsync(string tenantKey, Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task<TodoItem> CreateAsync(string tenantKey, Guid tenantId, string title, CancellationToken cancellationToken);
    Task<TodoItem?> UpdateAsync(string tenantKey, Guid tenantId, Guid id, string title, bool isCompleted, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string tenantKey, Guid tenantId, Guid id, CancellationToken cancellationToken);
}
