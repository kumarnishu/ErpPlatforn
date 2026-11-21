using BuildingBlocks.TenantMigrations;
using Microsoft.Extensions.DependencyInjection;
using Todos.Core.Interfaces;
using Todos.Core.Services;

namespace Todos.Core.Data;

public static class TodoServiceCollectionExtensions
{
    public static IServiceCollection AddTodosCore(this IServiceCollection services)
    {
        services.AddSingleton<ITodoDbContextFactory, TodoDbContextFactory>();
        services.AddScoped<ITodoService, TodoService>();
        return services;
    }

    public static IServiceCollection AddTodosTenantDatabaseMigrations(this IServiceCollection services) =>
        services.AddSingleton<ITenantMigrationContextFactory, TodoDbContextFactory>();
}
