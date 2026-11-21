# Tenant database migrations

`ITenantDatabaseMigrationService` applies EF Core migrations for selected feature codes. The host registers one `ITenantMigrationContextFactory` per service context. Each factory creates a `DbContext` for the supplied tenant key; the shared service calls `Database.MigrateAsync()` on each matching context.

```csharp
services.AddTenantDatabaseMigrations();
services.AddSingleton<ITenantMigrationContextFactory, OrdersDbContextFactory>();
```

Set the factory's `FeatureCode` to the code returned by the platform feature catalog. When that feature is selected, the coordinator applies its migrations. Unregistered feature codes are skipped and logged as warnings, which lets services add migration support as their contexts are created.
