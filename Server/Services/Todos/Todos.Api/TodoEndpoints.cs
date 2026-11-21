using BuildingBlocks.ApiResponses;
using BuildingBlocks.UserContext;
using Todos.Api.Models;
using Todos.Core.Interfaces;
using Todos.Core.Models;

namespace Todos.Api;

public static class TodoEndpoints
{
    public static IEndpointRouteBuilder MapTodoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/todos").RequireAuthorization();

        // List todos for the signed-in user's tenant.
        group.MapGet("/", async (ITodoService todos, IUserContextAccessor accessor, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (!TryGetActiveTenant(accessor, out var tenantKey, out var tenantId)) return Unauthorized(context);
            var items = await todos.GetForTenantAsync(tenantKey, tenantId, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyList<TodoItem>>.Success(items, context.TraceIdentifier));
        });

        // Get one todo from the signed-in user's tenant.
        group.MapGet("/{id:guid}", async (Guid id, ITodoService todos, IUserContextAccessor accessor, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (!TryGetActiveTenant(accessor, out var tenantKey, out var tenantId)) return Unauthorized(context);
            var item = await todos.GetAsync(tenantKey, tenantId, id, cancellationToken);
            return item is null
                ? NotFound(context)
                : Results.Ok(ApiResponse<TodoItem>.Success(item, context.TraceIdentifier));
        });

        // Create a todo in the signed-in user's tenant.
        group.MapPost("/", async (CreateTodoRequest request, ITodoService todos, IUserContextAccessor accessor, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (!TryGetActiveTenant(accessor, out var tenantKey, out var tenantId)) return Unauthorized(context);
            if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
                return Results.BadRequest(ApiResponse<object>.Failure(
                    new ApiError("INVALID_TODO_TITLE", "Title must be between 1 and 200 characters."), context.TraceIdentifier));

            var item = await todos.CreateAsync(tenantKey, tenantId, request.Title, cancellationToken);
            return Results.Created($"/api/v1/todos/{item.Id}", ApiResponse<TodoItem>.Success(item, context.TraceIdentifier));
        });

        // Update a todo's title and completion status.
        group.MapPut("/{id:guid}", async (Guid id, UpdateTodoRequest request, ITodoService todos, IUserContextAccessor accessor, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (!TryGetActiveTenant(accessor, out var tenantKey, out var tenantId)) return Unauthorized(context);
            if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
                return Results.BadRequest(ApiResponse<object>.Failure(
                    new ApiError("INVALID_TODO_TITLE", "Title must be between 1 and 200 characters."), context.TraceIdentifier));

            var item = await todos.UpdateAsync(tenantKey, tenantId, id, request.Title, request.IsCompleted, cancellationToken);
            return item is null
                ? NotFound(context)
                : Results.Ok(ApiResponse<TodoItem>.Success(item, context.TraceIdentifier));
        });

        // Soft-delete a todo from the signed-in user's tenant.
        group.MapDelete("/{id:guid}", async (Guid id, ITodoService todos, IUserContextAccessor accessor, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (!TryGetActiveTenant(accessor, out var tenantKey, out var tenantId)) return Unauthorized(context);
            return await todos.DeleteAsync(tenantKey, tenantId, id, cancellationToken) ? Results.NoContent() : NotFound(context);
        });

        return endpoints;
    }

    private static bool TryGetActiveTenant(IUserContextAccessor accessor, out string tenantKey, out Guid tenantId)
    {
        var user = accessor.Current;
        tenantKey = user?.TenantKey ?? string.Empty;
        tenantId = user?.TenantId ?? Guid.Empty;
        return user is { IsUserActive: true, IsTenantActive: true };
    }

    private static IResult Unauthorized(HttpContext context) => Results.Json(
        ApiResponse<object>.Failure(new ApiError("INACTIVE_ACCOUNT", "An active user and tenant are required."), context.TraceIdentifier),
        statusCode: StatusCodes.Status403Forbidden);

    private static IResult NotFound(HttpContext context) => Results.NotFound(
        ApiResponse<object>.Failure(new ApiError("TODO_NOT_FOUND", "The todo item was not found."), context.TraceIdentifier));
}
