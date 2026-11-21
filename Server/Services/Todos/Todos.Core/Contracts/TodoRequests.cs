namespace Todos.Core.Contracts;

public sealed record CreateTodoRequest(string Title);
public sealed record UpdateTodoRequest(string Title, bool IsCompleted);
