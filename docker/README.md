# Docker development environment

The four web apps run in containers. Docker Desktop with its Linux engine or Docker Engine with the Compose plugin is sufficient; a host Node.js or npm installation is not required.

## Start

From the repository root, run:

```sh
docker compose -f docker/docker-compose.yml up --build
```

Compose builds each app from its own `package.json` and lockfile, installs dependencies in the container, and mounts the app source for live reload. On Windows, file watching uses polling.

Open the apps at:

| App | URL |
| --- | --- |
| ERP | http://localhost:3000 |
| HRMS | http://localhost:3001 |
| Dashboard | http://localhost:3002 |
| Superadmin | http://localhost:3004 |

## Run selected apps

Pass one or more app names to Compose, for example:

```sh
docker compose -f docker/docker-compose.yml up --build erp-webapp dashboard-webapp
```

The other app names are `hrms-webapp` and `superadmin-webapp`. Omit names to run all apps.

## Stop and refresh dependencies

View logs with:

```sh
docker compose -f docker/docker-compose.yml logs -f
```

Stop the apps with:

```sh
docker compose -f docker/docker-compose.yml down
```

If a `package.json` or lockfile changes and dependencies need refreshing, recreate the containers and their dependency volumes:

```sh
docker compose -f docker/docker-compose.yml down
docker compose -f docker/docker-compose.yml up --build
```

