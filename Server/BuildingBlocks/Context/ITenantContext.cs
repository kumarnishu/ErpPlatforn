namespace BuildingBlocks.Context;

/// <summary>Tenant identity established by the trusted request pipeline.</summary>
public interface ITenantContext
{
    string? TenantId { get; }
}
