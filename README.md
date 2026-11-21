# ERP Platform

ERP Platform contains Platform and Todos .NET APIs plus four independent Vite/React apps.

## Quick start

Copy `.env.example` to `.env`, configure the authentication settings, then start the APIs and PostgreSQL:

```sh
docker compose -f docker/Infra.yml -f docker/server.yml up --build
```

Start the web apps with `docker compose -f docker/webapp.yml up --build`. For local development, API debugging, migrations, and manual requests, see:

- [Docker and Compose](docker/README.md)
- [Server setup and migrations](Server/README.md)
- [Frontend apps](webapp/README.md)
- [Manual API requests](ManualTesting/README.md)
