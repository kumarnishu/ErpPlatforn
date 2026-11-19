# Server

The server contains shared .NET building blocks for APIs. Add service projects under `Services/` as the domain is developed.

## Layout

```text
Server/
|-- BuildingBlocks/       # Shared API responses, JWT, middleware, Swagger
|-- BuildingBlocks/GoogleStorage/ # Tenant-scoped Google Cloud Storage adapter
|-- Services/Platform/   # Central platform API host
|-- Services/Todos/      # Tenant-scoped todos API
|-- Directory.Build.props
|-- Server.slnx
`-- README.md
```

Shared projects can be built from this directory with the .NET 10 SDK using `dotnet build Server.slnx`.

## Platform API

Run the API from this directory with `dotnet run --project Services/Platform/Platform.Api/Platform.Api.csproj`. It exposes `/health` and, when JWT authentication is configured, an authenticated `/api/v1/user-context` endpoint. Configure `Authentication:Issuer`, `Authentication:Audience`, and `Authentication:SigningKey` to validate JWTs issued by your application.

The Platform API includes a PostgreSQL tenant model in `Services/Platform/Platform.Api/Models/Tenant.cs` and `Data/PlatformDbContext.cs`. Set `ConnectionStrings__PlatformDatabase` to a PostgreSQL connection string to register the EF Core context. The `tenants` table uses UUID primary keys and a unique tenant key. Create migrations with `dotnet ef migrations add <Name> --project Services/Platform/Platform.Api/Platform.Api.csproj` after installing EF Core design-time tooling.

Platform authentication token creation is in `Services/Platform/Platform.Api/Services/AuthTokenService.cs`. It issues 15-minute access tokens and random 30-day refresh tokens with a SHA-256 hash for storing in the user session. Refresh-token validation, session persistence, and atomic hash rotation belong in the platform authentication flow.

Run the tenant-scoped Todos API with `dotnet run --project Services/Todos/Todos.Api/Todos.Api.csproj`. Its authenticated routes under `/api/v1/todos` support listing, creating, updating, and deleting todos. Todo data is currently held in process memory and is cleared when the API restarts.

Configuration is supplied through environment variables; there are no `appsettings` files. For Docker Compose, copy the repository's `.env.example` to `.env` and set `Authentication__Audience`, `Authentication__Issuer`, and `Authentication__SigningKey` as needed. The Compose stack loads `.env` into both APIs. Keep real signing keys in your local, ignored `.env` file.
