# Docker environments

Run these commands from the repository root with Docker Desktop's Linux engine or Docker Engine and the Compose plugin. Copy `.env.example` to `.env` first if you want to override the local defaults.

## Database and pgAdmin

Start PostgreSQL and pgAdmin without the APIs:

```sh
docker compose -f docker/Infra.yml up -d
```

PostgreSQL is available on `localhost:5432`; pgAdmin is at http://localhost:5050. The default local pgAdmin login is `admin@example.com` / `admin`. Register PostgreSQL in pgAdmin with host `postgres`, port `5432`, and the `POSTGRES_*` credentials from `.env` (defaults: database `erp_platform`, user `postgres`, password `postgres`). Data persists in Docker volumes named `postgres-data` and `pgadmin-data`.

## Server runtime stack

```sh
docker compose -f docker/Infra.yml -f docker/server.yml up --build
```

`Infra.yml` defines PostgreSQL and pgAdmin; `server.yml` builds and runs the Platform API and Todos API. Include both files so the services share a Compose network. Platform listens at http://localhost:5000 and Todos at http://localhost:5001. The APIs receive authentication settings from the root `.env` and connect to PostgreSQL using the `POSTGRES_*` values. PostgreSQL data persists in the `postgres-data` volume.

To run an API under the debugger on your machine while keeping PostgreSQL in Docker, start only the database service:

```sh
docker compose -f docker/Infra.yml up -d postgres
```

Then start `Platform.Api` from the IDE or with `dotnet run --project Server/Services/Platform/Platform.Api/Platform.Api.csproj`. For host-run APIs, configure `ConnectionStrings__PlatformDatabase` to use `Host=localhost` and the credentials from `.env`.

## Web app development stack

```sh
docker compose -f docker/webapp.yml up --build
```

Compose installs each app's dependencies in its container and mounts app source for live reload. On Windows, file watching uses polling. Open the apps at:

| App | URL |
| --- | --- |
| ERP | http://localhost:3000 |
| HRMS | http://localhost:3001 |
| Dashboard | http://localhost:3002 |
| Superadmin | http://localhost:3004 |
| pgAdmin | http://localhost:5050 (server stacks) |

To run selected apps, append service names, such as:

```sh
docker compose -f docker/webapp.yml up --build erp-webapp dashboard-webapp
```

The other web app service names are `hrms-webapp` and `superadmin-webapp`. Omit names to run all apps.

## Logs and shutdown

Use the same Compose file to view logs or stop that stack. To start the database infrastructure:

```sh
docker compose -f docker/Infra.yml logs -f
docker compose -f docker/Infra.yml down
```

For the runtime stack, include both files in the same order for logs or shutdown:

```sh
docker compose -f docker/Infra.yml -f docker/server.yml logs -f
docker compose -f docker/Infra.yml -f docker/server.yml down
```

Use `docker/webapp.yml` for the web app stack. If a web app's package files change, recreate the containers and dependency volumes to refresh dependencies:

```sh
docker compose -f docker/webapp.yml down
docker compose -f docker/webapp.yml up --build
```
