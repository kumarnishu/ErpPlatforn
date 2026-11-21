# Manual API requests

Run the `.http` requests with the VS Code REST Client or another compatible client. Start the APIs with the instructions in [docker/README.md](../docker/README.md), or run them directly. Default URLs: Platform `http://localhost:5000`; Todos `http://localhost:5001`.

Before testing, configure authentication and apply the Platform and tenant database migrations as described in [Server/README.md](../Server/README.md). `Platform.http` includes tenant creation, authentication, user, feature, refresh, and logout examples. Change the sample tenant key and credentials before creating another tenant.
