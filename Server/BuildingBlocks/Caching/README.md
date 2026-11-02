# Shared cache

The user-context and tenant-database-route caches use ASP.NET Core `IDistributedCache`, so the host can back them with Redis. Register the Redis implementation once in each service host, then register `IUserContextCache` and `ITenantDatabaseRouteCache` with their distributed implementations. Cache keys include both tenant and user or service identity. Route records contain a secret reference only, never database credentials.
