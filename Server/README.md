# Server

The server contains the .NET 10 APIs and shared building blocks. From the repository root, build the solution with:

```sh
dotnet build Server/Server.slnx
```

## Environment setup

Copy `.env.example` to `.env` at the repository root and configure:

- `Authentication__Issuer`
- `Authentication__Audience`
- `Authentication__SigningKey` (at least 32 bytes; keep real secrets out of source control)
- `ConnectionStrings__PlatformDatabase` (PostgreSQL connection string)
- `ConnectionStrings__TenantDatabaseAdmin` (PostgreSQL account used by Todos API to create tenant databases; requires `CREATEDB`)
- `ConnectionStrings__Redis` (shared by Platform and Todos APIs for session validation)

The APIs read settings from environment variables. `dotnet run` does not load the root `.env` automatically, so export the required values in your shell before starting an API. In Docker Compose, the root `.env` is loaded into both API containers and Compose overrides database and Redis hosts to the service names `postgres` and `redis`.

Start the Platform API directly from the repository root with:

```sh
dotnet run --project Server/Services/Platform/Platform.Api/Platform.Api.csproj
```

It listens on the configured ASP.NET Core URLs, or the local default. The Compose ports are `5000` for Platform and `5001` for Todos.

## PostgreSQL and EF Core migrations

`PlatformDbContext` maps tenants, users, sessions, and subscriptions in `Services/Platform/Platform.Api/Data/PlatformDbContext.cs`. The model includes `IsDeleted` flags and global query filters. Its initial migration is checked in under `Services/Platform/Platform.Api/Data/Migrations`.

Install the EF command-line tool once (the project includes the EF Core Design package):

```bash
dotnet tool install --global dotnet-ef --version 10.0.0
```

If `dotnet-ef` is already installed, update it with:

```bash
dotnet tool update --global dotnet-ef --version 10.0.0
```

Start the Dockerized PostgreSQL database from the repository root:

```bash
docker compose -f docker/docker-compose.yml up -d postgres
```

Run the migration commands below from a Bash shell at the repository root. Set the connection string in the same shell before invoking `dotnet ef`; use the credentials configured in the root `.env` (the defaults shown here are for local development):

```bash
export ConnectionStrings__PlatformDatabase='Host=localhost;Port=5432;Database=erp_platform;Username=postgres;Password=postgres'

dotnet ef migrations add InitialPlatformSchema \
  --project Server/Services/Platform/Platform.Api/Platform.Api.csproj \
  --startup-project Server/Services/Platform/Platform.Api/Platform.Api.csproj \
  --output-dir Data/Migrations

dotnet ef database update \
  --project Server/Services/Platform/Platform.Api/Platform.Api.csproj \
  --startup-project Server/Services/Platform/Platform.Api/Platform.Api.csproj
```

The connection string uses `localhost` because `dotnet ef` runs on the host; the API container uses `postgres` as its database hostname. Commit generated migration files so other environments can apply the same schema. For later model changes, create a new descriptive migration (for example `AddTenantLogo`) and run `dotnet ef database update` again. Avoid `EnsureCreated`; it does not work with the migrations workflow.

## APIs

- Platform: `POST /api/v1/tenants` creates a tenant and initial administrator; `/api/v1/auth` contains login, refresh, and logout routes; `POST /api/v1/tenants/users` creates a tenant user for the signed-in tenant administrator.
- Todos: authenticated routes under `/api/v1/todos` list, create, update, and soft-delete todos. Each signed tenant key is routed to its own PostgreSQL database named `erp_tenant_{tenantkey-lowercase}`. Todos API creates the database on the first authenticated todo request and applies its EF Core migrations automatically. Configure `ConnectionStrings__TenantDatabaseAdmin` with a PostgreSQL account that can create databases; for Compose it is set to the PostgreSQL service automatically.

Manual requests are in the root `ManualTesting/` directory. Docker Compose uses Redis for a shared session cache so Platform-issued tokens can be validated by Todos API. Outside Compose, configure the same Redis connection string for both APIs; if it is omitted, each process uses its own in-memory cache and tokens won't validate across API processes.
