# Docker development environment

The Platform API and four web apps run in containers. Docker Desktop with its Linux engine or Docker Engine with the Compose plugin is sufficient; a host .NET SDK, Node.js, or npm installation is not required.

## Start

From the repository root, run:

```sh
docker compose -f docker/docker-compose.yml up --build
```

Compose builds each app from its own `package.json` and lockfile, installs dependencies in the container, and mounts the app source for live reload. On Windows, file watching uses polling.

Compose starts a PostgreSQL container with a persistent volume and waits for it to become healthy before starting the Platform API. If `docker/platform-api.env` does not exist, copy `docker/platform-api.env.example` to create it. The default development connection string points the API at the Compose service name `postgres`. The database is available to host tools on `localhost:5432`.

The default development credentials are `platform` / `platform_dev_password` for the `platform` database. Change these together in Compose and `docker/platform-api.env` when needed. Do not commit local secrets; `platform-api.env` is ignored by Git. When the API starts, it applies the PostgreSQL EF Core migrations.

Open the apps at:

| App | URL |
| --- | --- |
| ERP | http://localhost:3000 |
| HRMS | http://localhost:3001 |
| Dashboard | http://localhost:3002 |
| Superadmin | http://localhost:3004 |
| Platform API health | http://localhost:5000/health |

## Run selected apps

Pass one or more app names to Compose, for example:

```sh
docker compose -f docker/docker-compose.yml up --build erp-webapp dashboard-webapp
```

The other app names are `hrms-webapp` and `superadmin-webapp`. `platform-api` can also be passed as a service name. Omit names to run all services.

## Stop and refresh dependencies

View logs with:

```sh
docker compose -f docker/docker-compose.yml logs -f
```

Stop the apps while keeping PostgreSQL data with:

```sh
docker compose -f docker/docker-compose.yml down
```

To also delete the database files, remove the Compose volumes with `docker compose -f docker/docker-compose.yml down -v`.

If a `package.json` or lockfile changes and dependencies need refreshing, recreate the containers and their dependency volumes:

```sh
docker compose -f docker/docker-compose.yml down
docker compose -f docker/docker-compose.yml up --build
```

Removing volumes also deletes the PostgreSQL data. The regular `down` command preserves database data.
