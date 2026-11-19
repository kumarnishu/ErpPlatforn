using Microsoft.EntityFrameworkCore;

namespace Todos.Api;

public sealed class TodoDbContext(DbContextOptions<TodoDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var todo = modelBuilder.Entity<TodoItem>();
        todo.ToTable("todos");
        todo.HasKey(x => x.Id);
        todo.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid");
        todo.Property(x => x.TenantId).HasColumnName("tenant_id").HasColumnType("uuid").IsRequired();
        todo.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        todo.Property(x => x.IsCompleted).HasColumnName("is_completed").IsRequired();
        todo.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        todo.Property(x => x.IsDeleted).HasColumnName("is_deleted").IsRequired();
        todo.HasIndex(x => x.TenantId);
        todo.HasQueryFilter(x => !x.IsDeleted);
    }
}
