namespace BuildingBlocks.Context;

public sealed record TenantContext(string? TenantId) : ITenantContext;
