# ERP Platform

## Project structure

```text
.
├── docker/
│   ├── docker-compose.yml   # Run one or all web apps for development
│   ├── Dockerfile           # Shared image definition; APP_DIR selects an app
│   └── README.md            # Docker setup, commands, and ports
├── Server/
│   └── Server.slnx          # .NET solution file
├── webapp/
│   ├── dashboard-webapp/    # Dashboard frontend
│   ├── erp-webapp/          # ERP frontend
│   ├── hrms-webapp/         # HRMS frontend
│   └── superadmin-webapp/   # Superadmin frontend
├── .dockerignore            # Excludes local files from Docker build context
└── README.md
```
