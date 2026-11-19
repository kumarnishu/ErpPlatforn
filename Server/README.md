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

The APIs read the `Authentication:*` settings from environment variables. The Platform API registers its PostgreSQL context only when `ConnectionStrings__PlatformDatabase` is set. The Docker Compose file loads the root `.env` into both API containers; when running in Docker, the database host in the connection string must be reachable from the container and cannot be `localhost` unless PostgreSQL runs in that same container.

Start the Platform API directly from the repository root with:

```sh
dotnet run --project Server/Services/Platform/Platform.Api/Platform.Api.csproj
```

It listens on the configured ASP.NET Core URLs, or the local default. The Compose ports are `5000` for Platform and `5001` for Todos.

## PostgreSQL and EF Core migrations

`PlatformDbContext` maps tenants, users, sessions, and subscriptions in `Services/Platform/Platform.Api/Data/PlatformDbContext.cs`. The model includes `IsDeleted` flags and global query filters. The repository currently has no checked-in migrations, so create the initial migration once PostgreSQL is available.

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
- Todos: authenticated routes under `/api/v1/todos` list, create, update, and soft-delete in-memory todos. Todo data is cleared when that process restarts.

Manual requests are in the root `ManualTesting/` directory. The two API processes currently use separate in-memory session caches; use a shared cache provider for Platform-issued tokens to validate across APIs.
