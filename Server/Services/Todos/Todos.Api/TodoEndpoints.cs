using BuildingBlocks.ApiResponses;
using BuildingBlocks.UserContext;

namespace Todos.Api;

public static class TodoEndpoints
{
    public static IEndpointRouteBuilder MapTodoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/todos").RequireAuthorization();

        group.MapGet("/", (TodoStore store, IUserContextAccessor accessor, HttpContext context) =>
        {
            if (!TryGetActiveTenant(accessor, out var tenantId)) return Unauthorized(context);
            return Results.Ok(ApiResponse<IEnumerable<TodoItem>>.Success(store.ForTenant(tenantId), context.TraceIdentifier));
        });

        group.MapGet("/{id:guid}", (Guid id, TodoStore store, IUserContextAccessor accessor, HttpContext context) =>
        {
            if (!TryGetActiveTenant(accessor, out var tenantId)) return Unauthorized(context);
            var item = store.Find(tenantId, id);
            return item is null
                ? NotFound(context)
                : Results.Ok(ApiResponse<TodoItem>.Success(item, context.TraceIdentifier));
        });

        group.MapPost("/", (CreateTodoRequest request, TodoStore store, IUserContextAccessor accessor, HttpContext context) =>
        {
            if (!TryGetActiveTenant(accessor, out var tenantId)) return Unauthorized(context);
            if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
                return Results.BadRequest(ApiResponse<object>.Failure(
                    new ApiError("INVALID_TODO_TITLE", "Title must be between 1 and 200 characters."), context.TraceIdentifier));

            var item = store.Add(tenantId, request.Title);
            return Results.Created($"/api/v1/todos/{item.Id}", ApiResponse<TodoItem>.Success(item, context.TraceIdentifier));
        });

        group.MapPut("/{id:guid}", (Guid id, UpdateTodoRequest request, TodoStore store, IUserContextAccessor accessor, HttpContext context) =>
        {
            if (!TryGetActiveTenant(accessor, out var tenantId)) return Unauthorized(context);
            if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
                return Results.BadRequest(ApiResponse<object>.Failure(
                    new ApiError("INVALID_TODO_TITLE", "Title must be between 1 and 200 characters."), context.TraceIdentifier));

            var item = store.Update(tenantId, id, request.Title, request.IsCompleted);
            return item is null
                ? NotFound(context)
                : Results.Ok(ApiResponse<TodoItem>.Success(item, context.TraceIdentifier));
        });

        group.MapDelete("/{id:guid}", (Guid id, TodoStore store, IUserContextAccessor accessor, HttpContext context) =>
        {
            if (!TryGetActiveTenant(accessor, out var tenantId)) return Unauthorized(context);
            return store.Remove(tenantId, id) ? Results.NoContent() : NotFound(context);
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
