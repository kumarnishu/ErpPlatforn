# ERP Platform

## Project structure

```text
.
|-- docker/                 # Docker Compose and development image
|-- Server/
|   |-- BuildingBlocks/     # Shared context, auth, cache, HTTP, tenancy
|   |-- Services/           # Platform service and microservice template
|   |-- manual-tests/       # Hand-run .http requests
|   |-- tests/
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

Each folder under `webapp/` is an independent Vite/React application with its own dependencies and build configuration. Under `Server/`, `BuildingBlocks/` contains shared cross-service code, while `Services/` contains independently deployable services. See [Server/README.md](Server/README.md) for the shared service architecture and [webapp/README.md](webapp/README.md) for frontend structure.
