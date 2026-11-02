# Shared service architecture

This server layout is intended for independently deployable microservices. Each service owns its domain, database context, migrations, and database provider. Shared libraries hold only contracts and cross-cutting behavior that should stay consistent across services.

## Layout

```text
Server/
|-- BuildingBlocks/
|   |-- Context/          # IUserContext and ITenantContext contracts
|   |-- Authentication/   # Shared claim mapping and HTTP context registration
|   |-- Caching/          # Redis-compatible user and route caches
|   |-- Http/             # API response envelope and global error middleware
|   `-- MultiTenancy/     # Tenant/service database routing contracts
|-- Services/
|   |-- Platform/         # Central tenant registry and platform capabilities
|   |-- ServiceTemplate/  # Copy this boundary for each business microservice
|   `-- Todo/             # First service created from the template
|-- manual-tests/         # Hand-run HTTP Client request files
|-- tests/
|-- Directory.Build.props
|-- Server.slnx
`-- README.md
```

Create future service folders such as Employees, Products, Orders, Inventory, or Customers from `Services/ServiceTemplate`. Each gets its own API, Application, Domain, and Infrastructure projects and its own database context and migrations. `Services/Todo` demonstrates the copied structure and adds a user-owned Todo domain entity and repository contract.

## Request and database routing

1. The service validates the bearer token using the shared identity-provider configuration.
2. `AddSharedRequestContext()` exposes the validated user ID, roles, and tenant ID as scoped `IUserContext` and `ITenantContext` services.
3. The service authorizes that user for the selected tenant before accessing tenant data.
4. `ITenantDatabaseResolver` resolves a route using both the tenant ID and the service's stable service ID. One tenant can therefore map to different databases for different microservices.
5. The service's infrastructure retrieves credentials from a secret store using `SecretReference`, then creates its own provider-specific DbContext and connection pool.

The Platform service owns the authoritative central registry and tenant provisioning lifecycle. Redis may cache short-lived user authorization snapshots and non-secret route metadata. Cache keys include tenant plus user or service identity. Do not cache tokens or plaintext database credentials, and invalidate user context after membership or role changes.

## Shared building blocks

- **Context**: stable user and tenant context interfaces consumed by application code.
- **Authentication**: shared claim names and HTTP request context adapters. The identity provider is not selected yet; add token validation and shared authorization policies here once selected.
- **Caching**: user-context and tenant/service route caches backed by `IDistributedCache`; configure the Redis provider in each service host.
- **Http**: common `ApiResponse<T>` and `ApiError` format plus exception middleware. Expected `ApiException` values keep their safe public message; unexpected exceptions are logged and returned as a generic 500 response with a trace ID.
- **MultiTenancy**: tenant database route and resolver contracts. Service-specific persistence code owns the actual DbContext and provider configuration.

## Dependency direction

`Domain` has no shared-library dependencies. `Application` depends on its service Domain and shared Context/MultiTenancy contracts. `Infrastructure` implements persistence and integration details. `Api` composes the service layers with shared HTTP, authentication, caching, and tenancy behavior. Avoid putting business entities or service-specific database models in `BuildingBlocks`.

The Platform, ServiceTemplate, and Todo APIs currently expose a health endpoint and shared request-context/error-handler registration. Todo has a domain entity and repository contract, but no CRUD endpoints or persistence implementation yet. Provider-specific token validation, Redis host configuration, database providers, and migrations remain to be selected and implemented.

## Build

Use the .NET 10 SDK and run `dotnet build Server.slnx` from this directory.
