# Shared server building blocks

`Server/` currently contains only reusable .NET libraries. Service implementations have been removed so service boundaries and APIs can be redesigned independently.

## Layout

```text
Server/
|-- BuildingBlocks/
|   |-- Authentication/   # JWT validation and HTTP request context adapters
|   |-- Caching/          # Distributed user context and tenant route caches
|   |-- Context/          # User and tenant context contracts
|   |-- Http/             # API response types and exception middleware
|   `-- MultiTenancy/     # Tenant/service database route contracts
|-- Directory.Build.props
|-- Server.slnx
`-- README.md
```

## Building blocks

- **Context** defines stable user and tenant context interfaces.
- **Authentication** provides shared JWT bearer validation, claim names, and adapters that expose the authenticated request as `IUserContext` and `ITenantContext`.
- **Caching** provides `IDistributedCache`-backed user context and tenant database route caches.
- **Http** defines the common `ApiResponse<T>` and `ApiError` format and global exception middleware.
- **MultiTenancy** defines tenant database route and resolver contracts. A future service owns its DbContext, provider configuration, and persistence implementation.

## Dependency direction

`Context` and `MultiTenancy` are contracts for service application code. `Authentication`, `Caching`, and `Http` are host-level integrations. Keep business entities, service-specific persistence models, and API endpoints in the services that are added later, rather than in `BuildingBlocks`.

## Build

Use the .NET 10 SDK and run `dotnet build Server.slnx` from this directory.
