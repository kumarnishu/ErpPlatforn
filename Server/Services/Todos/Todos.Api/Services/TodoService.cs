using Microsoft.EntityFrameworkCore;
using Todos.Api.Data;
using Todos.Api.Interfaces;
using Todos.Api.Models;

namespace Todos.Api.Services;

public sealed class TodoService(TodoDbContext db) : ITodoService
{
    public async Task<IReadOnlyList<TodoItem>> GetForTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await db.TodoItems.AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<TodoItem?> GetAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        db.TodoItems.AsNoTracking().SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == id, cancellationToken);

    public async Task<TodoItem> CreateAsync(Guid tenantId, string title, CancellationToken cancellationToken)
    {
        var item = new TodoItem { TenantId = tenantId, Title = title.Trim() };
        db.TodoItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<TodoItem?> UpdateAsync(
        Guid tenantId, Guid id, string title, bool isCompleted, CancellationToken cancellationToken)
    {
        var item = await db.TodoItems.SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == id, cancellationToken);
        if (item is null) return null;

        item.Title = title.Trim();
        item.IsCompleted = isCompleted;
        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<bool> DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var item = await db.TodoItems.SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == id, cancellationToken);
        if (item is null) return false;

        item.IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
