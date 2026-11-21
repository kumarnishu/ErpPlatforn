namespace BuildingBlocks.TenantMigrations;

/// <summary>Applies migrations for selected tenant features.</summary>
public interface ITenantDatabaseMigrationService
{
    Task ApplySelectedAsync(string tenantKey, IEnumerable<string> featureCodes, CancellationToken cancellationToken);
}
