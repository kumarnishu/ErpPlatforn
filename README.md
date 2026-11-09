# ERP Platform

## Project structure

```text
.
|-- docker/                 # Docker Compose and development image
|-- Server/
|   |-- BuildingBlocks/     # Shared context, auth, cache, HTTP, tenancy
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

Each folder under `webapp/` is an independent Vite/React application with its own dependencies and build configuration. `Server/` currently contains only shared .NET building blocks; service implementations were removed for redesign. See [Server/README.md](Server/README.md) for the shared server libraries and [webapp/README.md](webapp/README.md) for frontend structure.
