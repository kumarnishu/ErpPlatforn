namespace BuildingBlocks.MultiTenancy;

/// <summary>Non-secret tenant database metadata for a specific microservice.</summary>
public sealed record TenantDatabaseRoute(
    string TenantId,
    string ServiceId,
    string Provider,
    string DatabaseName,
    string SecretReference);
