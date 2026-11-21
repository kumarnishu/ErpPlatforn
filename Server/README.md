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

The APIs read the `Authentication:*` settings from environment variables. The Platform API stores platform data in the shared `PlatformDB`. The Todos API uses the same PostgreSQL server credentials and selects a database named `tenant_<tenant-key-lowercase>` for each request. Create each tenant database and apply the Todos migration before using that tenant. The Docker Compose file sets the shared connection string for both API containers; when running in Docker, the database host must be reachable from the container and cannot be `localhost` unless PostgreSQL runs in that same container.

Start the Platform API directly from the repository root with:

```sh
dotnet run --project Server/Services/Platform/Platform.Api/Platform.Api.csproj
```

It listens on the configured ASP.NET Core URLs, or the local default. The Compose ports are `5000` for Platform and `5001` for Todos.

## PostgreSQL and EF Core migrations

`PlatformDbContext` stores platform data in shared `PlatformDB`. `TodoDbContext` stores Todos in a tenant-specific database selected by tenant key. Todos use a separate migration history table (`__TodosMigrationsHistory`) in each tenant database.

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

Run EF commands from a Bash shell at the repository root. Set the connection string in the same shell before invoking `dotnet ef`; use the credentials configured in the root `.env` (the defaults shown here are for local development):

```bash
export ConnectionStrings__PlatformDatabase='Host=localhost;Port=5432;Database=erp_platform;Username=postgres;Password=postgres'
```

### Platform migrations

Create and apply Platform migrations manually against the shared `PlatformDB`. Replace `AddYourChange` with a descriptive migration name:

```bash
dotnet ef migrations add AddYourChange \
  --project Server/Services/Platform/Platform.Api/Platform.Api.csproj \
  --startup-project Server/Services/Platform/Platform.Api/Platform.Api.csproj \
  --output-dir Data/Migrations

dotnet ef database update \
  --project Server/Services/Platform/Platform.Api/Platform.Api.csproj \
  --startup-project Server/Services/Platform/Platform.Api/Platform.Api.csproj
```

### Tenant service migrations

For tenant services such as Todos, create and commit migration files with the CLI, but do not apply them using `dotnet ef database update`. The tenant feature update endpoint applies migrations for selected features that have a registered `ITenantMigrationContextFactory`. For example, Platform currently registers the Todos factory, whose feature code is `todos`.

Todos needs `TenantKey` at design time to select the tenant database. It must contain 2-4 ASCII letters or digits. Set it to a representative tenant key (this example uses `QA1`) when creating a Todos migration:

```bash
export TenantKey=QA1

dotnet ef migrations add AddYourChange \
  --project Server/Services/Todos/Todos.Core/Todos.Core.csproj \
  --startup-project Server/Services/Todos/Todos.Api/Todos.Api.csproj \
  --context Todos.Core.Data.TodoDbContext \
  --output-dir Data/Migrations
```

Commit the generated migration files with the model changes. A tenant admin updates selected features through `PUT /api/v1/tenants/me/features`; the endpoint applies migrations for selected service features that are not yet marked `IsDbCreated`, then records the feature rows in PlatformDB only after every selected feature has a registered migration context and migration succeeds. Otherwise it returns `503 FEATURE_MIGRATION_NOT_APPLIED` without saving the feature selection. Repeating the feature update skips migrations already marked as initialized. Ensure the tenant database exists before the feature is selected. The connection string uses `localhost` because `dotnet ef` runs on the host; API containers use `postgres` as the database hostname. Avoid `EnsureCreated`; it does not work with the migrations workflow.

## APIs

- Platform: `POST /api/v1/tenants` creates a tenant and initial administrator; `/api/v1/auth` contains login, refresh, and logout routes; `POST /api/v1/tenants/users` creates a tenant user for the signed-in tenant administrator.
- Todos: authenticated routes under `/api/v1/todos` list, create, update, and soft-delete tenant-scoped todos stored in PostgreSQL.

Manual requests are in the root `ManualTesting/` directory. The two API processes currently use separate in-memory session caches; use a shared cache provider for Platform-issued tokens to validate across APIs.
