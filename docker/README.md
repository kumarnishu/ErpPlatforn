# Docker development environment

The four web apps run inside Node containers. A host Node.js or npm installation is not required; Docker Desktop with its Linux engine or Docker Engine with the Compose plugin is sufficient.

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

## Run only selected apps

Pass one or more service names to Compose. For example, to run only ERP:

```sh
docker compose -f docker/docker-compose.yml up --build erp-webapp
```

Use `hrms-webapp`, `dashboard-webapp`, or `superadmin-webapp` for the other apps. Omit the service name to run all four.

## Build and run one image directly

The shared Dockerfile can also build an individual app without Compose. For example, build and run ERP:

```sh
docker build -f docker/Dockerfile --build-arg APP_DIR=erp-webapp -t erp-webapp .
docker run --rm -p 3000:3000 erp-webapp
```

For the other apps, set `APP_DIR` to `hrms-webapp`, `dashboard-webapp`, or `superadmin-webapp`, and map their configured ports: 3001, 3002, or 3004.

Run the same command with `-d` to start in the background. View logs with:

```sh
docker compose -f docker/docker-compose.yml logs -f
```

Stop the apps with:

```sh
docker compose -f docker/docker-compose.yml down
```

If a `package.json` or lockfile changes and dependencies need refreshing, recreate the containers and their dependency volumes:

```sh
docker compose -f docker/docker-compose.yml down -v
docker compose -f docker/docker-compose.yml up --build
```

The `-v` option removes Compose-managed volumes, including container-installed `node_modules`; it does not remove project files.
