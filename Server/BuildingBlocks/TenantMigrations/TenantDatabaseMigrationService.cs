using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.TenantMigrations;

/// <summary>Runs migrations for feature contexts registered by services.</summary>
public sealed class TenantDatabaseMigrationService(
    IEnumerable<ITenantMigrationContextFactory> contextFactories,
    ILogger<TenantDatabaseMigrationService> logger) : ITenantDatabaseMigrationService
{
    private readonly IReadOnlyDictionary<string, ITenantMigrationContextFactory> _factories =
        contextFactories.ToDictionary(x => x.FeatureCode, StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlySet<string>> ApplySelectedAsync(
        string tenantKey,
        IEnumerable<string> featureCodes,
        CancellationToken cancellationToken)
    {
        var applied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var featureCode in featureCodes.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!_factories.TryGetValue(featureCode, out var factory))
            {
                logger.LogWarning("No tenant database migration context is registered for feature {FeatureCode}.", featureCode);
                continue;
            }

            await using var db = factory.Create(tenantKey);
            await db.Database.MigrateAsync(cancellationToken);
            applied.Add(featureCode);
        }

        return applied;
    }
}
