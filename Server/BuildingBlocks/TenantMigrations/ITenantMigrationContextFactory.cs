using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.TenantMigrations;

/// <summary>Creates a service DbContext targeting the database for a tenant key.</summary>
public interface ITenantMigrationContextFactory
{
    string FeatureCode { get; }
    DbContext Create(string tenantKey);
}
