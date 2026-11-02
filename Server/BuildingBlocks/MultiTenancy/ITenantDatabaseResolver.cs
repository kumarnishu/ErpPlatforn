namespace BuildingBlocks.MultiTenancy;

/// <summary>Resolves a tenant's database route for one microservice.</summary>
public interface ITenantDatabaseResolver
{
    ValueTask<TenantDatabaseRoute> ResolveAsync(
        string tenantId,
        string serviceId,
        CancellationToken cancellationToken = default);
}
