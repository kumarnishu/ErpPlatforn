# ERP Platform

## Project structure

```text
.
|-- docker/                 # Docker Compose and development image
|-- Server/
|   |-- BuildingBlocks/     # Shared API responses, JWT, middleware, Swagger libraries
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

Each folder under `webapp/` is an independent Vite/React application with its own dependencies and build configuration. `Server/` contains shared .NET building blocks. See [Server/README.md](Server/README.md) for server setup and [webapp/README.md](webapp/README.md) for frontend structure.

## Run with Docker

The development Compose stack starts the web apps:

```sh
docker compose -f docker/docker-compose.yml up --build
```

