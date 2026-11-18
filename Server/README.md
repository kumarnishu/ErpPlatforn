# Server

The server contains shared .NET building blocks for APIs. Add service projects under `Services/` as the domain is developed.

## Layout

```text
Server/
|-- BuildingBlocks/       # Shared API responses, JWT, middleware, Swagger
|-- Directory.Build.props
|-- Server.slnx
`-- README.md
```

Shared projects can be built from this directory with the .NET 10 SDK using `dotnet build Server.slnx`.
