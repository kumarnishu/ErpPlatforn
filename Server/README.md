# Server

The server contains shared .NET building blocks and a `Platform` service. The Platform service currently exposes tenant signup; other services can be added under `Services/` as the domain is redesigned.

## Layout

```text
Server/
|-- BuildingBlocks/       # Shared request context, authentication, cache, HTTP, tenancy contracts
|-- Services/Platform/
|   |-- Platform.Api/     # ASP.NET Core controller host
|   |-- Platform.Application/ # Signup use case and repository contracts
|   |-- Platform.Domain/  # Tenant, user, and membership entities
|   `-- Platform.Infrastructure/ # EF Core repository, PostgreSQL, password hashing
|-- Directory.Build.props
|-- Server.slnx
`-- README.md
```

## Tenant signup

`POST /api/v1/signup` accepts `tenantName`, `tenantKey`, `adminName`, `adminEmail`, and `password`. It creates an active tenant, a local platform user with an ASP.NET Core Identity password hash, and that user's `TenantAdmin` membership. The tenant, user, and membership are persisted together. Tenant keys and normalized email addresses are unique.

Swagger UI is available at `/swagger` and the OpenAPI document at `/swagger/v1/swagger.json`.

The reusable Swagger setup lives in `BuildingBlocks.Http`. For another API project that references this building block, register it with `builder.Services.AddApiSwagger("API title", "API description")` and enable it with `app.UseApiSwagger("API title")`.

Example request:

```json
{
  "tenantName": "Northwind",
  "tenantKey": "northwind",
  "adminName": "Morgan Lee",
  "adminEmail": "morgan@example.com",
  "password": "use-a-long-unique-password"
}
```

Configure `ConnectionStrings:PlatformDatabase` or `PLATFORM_DB_CONNECTION` with a PostgreSQL connection string before using the signup route. `/health` remains available without a database; signup returns 503 until persistence is configured. The API does not issue a login token yet.

Apply the initial schema from `Server/` after setting `PLATFORM_DB_CONNECTION` in the shell:

```sh
dotnet ef database update --project Services/Platform/Platform.Infrastructure/Platform.Infrastructure.csproj --startup-project Services/Platform/Platform.Api/Platform.Api.csproj --context PlatformDbContext
```

Run the API with `dotnet run --project Services/Platform/Platform.Api/Platform.Api.csproj`. It listens on the configured ASP.NET Core URL, and `/health` reports service availability.

## Build

Use the .NET 10 SDK and run the Platform application tests before building the API from this directory:

```sh
dotnet test Services/Platform/Platform.Application.Tests/Platform.Application.Tests.csproj
dotnet build Services/Platform/Platform.Api/Platform.Api.csproj
```

The Docker build in `docker/Server.Dockerfile` also runs these tests before publishing the API image.
