namespace Platform.Application.Abstractions;

public sealed class TenantSignupConflictException(string conflict) : Exception(conflict)
{
    public string Conflict { get; } = conflict;
}
