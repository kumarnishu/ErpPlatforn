# Docker development environment

The API services and four web apps run in containers. A host .NET SDK, Node.js, or npm installation is not required; Docker Desktop with its Linux engine or Docker Engine with the Compose plugin is sufficient.

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
| Platform API | http://localhost:5000/health |

## Run only selected apps

Pass one or more service names to Compose. For example, to run only ERP and the Platform API:

```sh
docker compose -f docker/docker-compose.yml up --build erp-webapp platform-api
```

Use `hrms-webapp`, `dashboard-webapp`, or `superadmin-webapp` for the other apps. Omit the service name to run all four.

The `platform-api` service builds the Platform .NET 10 API project from the repository root. It listens on container port 8080 and publishes its health endpoint on host port 5000. The `ServiceTemplate` project is a starter template and is not included in the default Compose stack.

To build a different API project with the shared server Dockerfile, provide its project path relative to `Server/`:

```sh
docker build -f docker/Server.Dockerfile --build-arg API_PROJECT=Services/ServiceTemplate/ServiceTemplate.Api/ServiceTemplate.Api.csproj --build-arg API_DLL=ServiceTemplate.Api.dll -t service-template-api .
```

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
