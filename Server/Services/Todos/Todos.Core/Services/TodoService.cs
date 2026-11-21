using Microsoft.EntityFrameworkCore;
using Todos.Core.Data;
using Todos.Core.Interfaces;
using Todos.Core.Models;

namespace Todos.Core.Services;

/// <summary>Handles todo persistence using the tenant's database.</summary>
public sealed class TodoService(ITodoDbContextFactory dbFactory) : ITodoService
{
    public async Task<IReadOnlyList<TodoItem>> GetForTenantAsync(string tenantKey, Guid tenantId, CancellationToken cancellationToken)
    {
        await using var db = dbFactory.Create(tenantKey);
        return await db.TodoItems.AsNoTracking().Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task<TodoItem?> GetAsync(string tenantKey, Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        await using var db = dbFactory.Create(tenantKey);
        return await db.TodoItems.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, cancellationToken);
    }

    public async Task<TodoItem> CreateAsync(string tenantKey, Guid tenantId, string title, CancellationToken cancellationToken)
    {
        await using var db = dbFactory.Create(tenantKey);
        var item = new TodoItem { TenantId = tenantId, Title = title.Trim() };
        db.TodoItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<TodoItem?> UpdateAsync(string tenantKey, Guid tenantId, Guid id, string title, bool isCompleted, CancellationToken cancellationToken)
    {
        await using var db = dbFactory.Create(tenantKey);
        var item = await db.TodoItems.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, cancellationToken);
        if (item is null) return null;
        item.Title = title.Trim();
        item.IsCompleted = isCompleted;
        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<bool> DeleteAsync(string tenantKey, Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        await using var db = dbFactory.Create(tenantKey);
        var item = await db.TodoItems.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, cancellationToken);
        if (item is null) return false;
        item.IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
