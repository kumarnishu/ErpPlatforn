# Docker environments

Docker Compose is split into server development, server runtime, and web app stacks. Run commands from the repository root. Docker Desktop with its Linux engine or Docker Engine with the Compose plugin is sufficient.

## Server development stack

```sh
docker compose -f docker/server.dev.yml up --build
```

This starts PostgreSQL, pgAdmin, and both APIs with `dotnet watch`. API source is mounted from `Server/` for live reload. Set `POSTGRES_*` and `PGADMIN_*` values in `.env` to override local defaults.

## Server runtime stack

```sh
docker compose -f docker/server.yml up --build
```

This uses the published API containers with PostgreSQL and pgAdmin. PostgreSQL data persists in the `postgres-data` volume. pgAdmin is available at http://localhost:5050; default local login is `admin@example.com` / `admin`. Register the server using host `postgres`, port `5432`, and the database credentials from `.env` (defaults: database `erp_platform`, user `postgres`, password `postgres`).

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

Use the same Compose file to view logs or stop that stack:

```sh
docker compose -f docker/server.dev.yml logs -f
docker compose -f docker/server.dev.yml down
```

Replace `server.dev.yml` with `server.yml` or `webapp.yml` as needed. If a web app's package files change, recreate the containers and dependency volumes to refresh dependencies:

```sh
docker compose -f docker/webapp.yml down
docker compose -f docker/webapp.yml up --build
```
