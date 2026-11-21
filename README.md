# ERP Platform

## Project structure

```text
.
|-- docker/                 # Docker Compose and development image
|-- ManualTesting/          # Ready-to-run .http requests for the APIs
|-- Server/
|   |-- BuildingBlocks/     # Shared API responses, JWT, middleware, Swagger libraries
|   |-- Services/           # Platform and Todos APIs
|   |-- Directory.Build.props
|   |-- Server.slnx
|   `-- README.md
|-- webapp/
|   |-- dashboard-webapp/
|   |-- erp-webapp/
|   |-- hrms-webapp/
|   |-- superadmin-webapp/
|   `-- README.md
|-- .dockerignore
|-- .gitignore
`-- README.md
```

Each folder under `webapp/` is an independent Vite/React application with its own dependencies and build configuration. See [Server/README.md](Server/README.md) for API, PostgreSQL, environment, and migration setup, [ManualTesting/README.md](ManualTesting/README.md) for HTTP requests, and [webapp/README.md](webapp/README.md) for frontend structure.

## Local environment

Copy `.env.example` to `.env` in the repository root. Configure the authentication issuer, audience, and signing key. To use the Platform API, set `ConnectionStrings__PlatformDatabase` to a reachable PostgreSQL database; see [Server/README.md](Server/README.md#postgresql-and-ef-core-migrations) for the exact EF Core commands to create and apply migrations. Docker Compose starts the APIs and web apps, but PostgreSQL must be provided separately. When the APIs run in containers, use a database host address that is reachable from those containers.

## Run with Docker

The development Compose stack starts the APIs and web apps:

```sh
docker compose -f docker/server.dev.yml up --build
```

