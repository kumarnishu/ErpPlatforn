using BuildingBlocks.ApiResponses;
using BuildingBlocks.UserContext;
using Microsoft.EntityFrameworkCore;

namespace Todos.Api;

public static class TodoEndpoints
{
    public static IEndpointRouteBuilder MapTodoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/todos").RequireAuthorization();

        group.MapGet("/", async (TodoDbContext db, IUserContextAccessor accessor, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (!TryGetActiveTenant(accessor, out var tenantId)) return Unauthorized(context);
            var items = await db.Todos.AsNoTracking()
                .Where(item => item.TenantId == tenantId)
                .OrderByDescending(item => item.CreatedAtUtc)
                .ToListAsync(cancellationToken);
            return Results.Ok(ApiResponse<IEnumerable<TodoItem>>.Success(items, context.TraceIdentifier));
        });

        group.MapGet("/{id:guid}", async (Guid id, TodoDbContext db, IUserContextAccessor accessor, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (!TryGetActiveTenant(accessor, out var tenantId)) return Unauthorized(context);
            var item = await db.Todos.AsNoTracking()
                .SingleOrDefaultAsync(todo => todo.Id == id && todo.TenantId == tenantId, cancellationToken);
            return item is null
                ? NotFound(context)
                : Results.Ok(ApiResponse<TodoItem>.Success(item, context.TraceIdentifier));
        });

        group.MapPost("/", async (CreateTodoRequest request, TodoDbContext db, IUserContextAccessor accessor, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (!TryGetActiveTenant(accessor, out var tenantId)) return Unauthorized(context);
            if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
                return Results.BadRequest(ApiResponse<object>.Failure(
                    new ApiError("INVALID_TODO_TITLE", "Title must be between 1 and 200 characters."), context.TraceIdentifier));

            var item = new TodoItem { TenantId = tenantId, Title = request.Title.Trim() };
            db.Todos.Add(item);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/v1/todos/{item.Id}", ApiResponse<TodoItem>.Success(item, context.TraceIdentifier));
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateTodoRequest request, TodoDbContext db, IUserContextAccessor accessor, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (!TryGetActiveTenant(accessor, out var tenantId)) return Unauthorized(context);
            if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
                return Results.BadRequest(ApiResponse<object>.Failure(
                    new ApiError("INVALID_TODO_TITLE", "Title must be between 1 and 200 characters."), context.TraceIdentifier));

            var item = await db.Todos.SingleOrDefaultAsync(todo => todo.Id == id && todo.TenantId == tenantId, cancellationToken);
            if (item is null) return NotFound(context);

            item.Title = request.Title.Trim();
            item.IsCompleted = request.IsCompleted;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ApiResponse<TodoItem>.Success(item, context.TraceIdentifier));
        });

        group.MapDelete("/{id:guid}", async (Guid id, TodoDbContext db, IUserContextAccessor accessor, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (!TryGetActiveTenant(accessor, out var tenantId)) return Unauthorized(context);
            var item = await db.Todos.SingleOrDefaultAsync(todo => todo.Id == id && todo.TenantId == tenantId, cancellationToken);
            if (item is null) return NotFound(context);

            item.IsDeleted = true;
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        });

        return endpoints;
    }

    private static bool TryGetActiveTenant(IUserContextAccessor accessor, out Guid tenantId)
    {
        var user = accessor.Current;
        tenantId = user?.TenantId ?? Guid.Empty;
        return user is { IsUserActive: true, IsTenantActive: true };
    }

    private static IResult Unauthorized(HttpContext context) => Results.Json(
        ApiResponse<object>.Failure(new ApiError("INACTIVE_ACCOUNT", "An active user and tenant are required."), context.TraceIdentifier),
        statusCode: StatusCodes.Status403Forbidden);

    private static IResult NotFound(HttpContext context) => Results.NotFound(
        ApiResponse<object>.Failure(new ApiError("TODO_NOT_FOUND", "The todo item was not found."), context.TraceIdentifier));
}
