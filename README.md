# ERP Platform

## Project structure

```text
.
|-- docker/                 # Docker Compose and development image
|-- Server/
|   |-- BuildingBlocks/     # Shared context, auth, cache, HTTP, tenancy
|   |-- Services/Platform/  # Tenant signup API and persistence
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

Each folder under `webapp/` is an independent Vite/React application with its own dependencies and build configuration. `Server/` contains shared .NET building blocks and a Platform service for tenant signup. See [Server/README.md](Server/README.md) for server setup and [webapp/README.md](webapp/README.md) for frontend structure.
