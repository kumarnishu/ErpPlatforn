# Tenant database routing

Each microservice resolves a route using both `tenantId` and its own stable `serviceId`; the same tenant can therefore use different databases for Employees, Orders, Inventory, or other services. The Platform service owns the authoritative tenant/database registry. Services may cache route metadata briefly, but must retrieve credentials from a secret store using `SecretReference`.

Authenticate the user and authorize access to the selected tenant before resolving a route. Do not share a DbContext or connection pool across tenants unless the provider's pool key includes the complete tenant-specific connection identity.
