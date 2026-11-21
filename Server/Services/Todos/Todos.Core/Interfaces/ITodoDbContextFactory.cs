using Todos.Core.Data;

namespace Todos.Core.Interfaces;

/// <summary>Creates a TodoDbContext for the database selected by a tenant key.</summary>
public interface ITodoDbContextFactory
{
    TodoDbContext Create(string tenantKey);
}
