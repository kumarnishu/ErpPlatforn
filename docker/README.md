# Docker development environment

The Platform API and four web apps run in containers. Docker Desktop with its Linux engine or Docker Engine with the Compose plugin is sufficient; a host .NET SDK, Node.js, or npm installation is not required.

## Start

From the repository root, run:

```sh
docker compose -f docker/docker-compose.yml up --build
```

Compose builds each app from its own `package.json` and lockfile, installs dependencies in the container, and mounts the app source for live reload. On Windows, file watching uses polling.

The Platform API uses SQL Server Express in the Compose stack. Compose starts the database, waits for its health check, and applies the Platform database migrations when the API starts. The database files persist in the `platform-sql-data` volume.

Start all services from the repository root:

```sh
docker compose -f docker/docker-compose.yml up --build
```

The local development SQL Server password defaults to `LocalDev_StrongPassw0rd!`. To use a different password, set `MSSQL_SA_PASSWORD` in your shell before running Compose. SQL Server requires a strong password. The database is available on `localhost:1433`; the API connects to it using the Compose service name.

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

Stop the apps while keeping SQL Server data with:

```sh
docker compose -f docker/docker-compose.yml down
```

To also delete the database files, remove the Compose volumes with `docker compose -f docker/docker-compose.yml down -v`.

If a `package.json` or lockfile changes and dependencies need refreshing, recreate the containers and their dependency volumes:

```sh
docker compose -f docker/docker-compose.yml down
docker compose -f docker/docker-compose.yml up --build
```

Removing volumes also deletes the SQL Server data. The regular `down` command preserves database data.
