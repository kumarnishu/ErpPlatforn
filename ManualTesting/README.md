# Manual API requests

These `.http` files can be run with the VS Code REST Client extension or another HTTP client that supports the `.http` request format.

Start the APIs with Docker Compose from `docker/` or run the projects directly. The default local URLs are Platform API `http://localhost:5000` and Todos API `http://localhost:5001`.

PlatformDB must have the Platform migration applied, and each tenant database (`tenant_<tenant-key-lowercase>`) must have the Todos migration applied. Set `Authentication__Issuer`, `Authentication__Audience`, and `Authentication__SigningKey` for login and authenticated routes. Both APIs keep their development session cache in memory, so a Platform-issued token will not validate in Todos across separate API processes unless both are configured with a shared cache provider.

`Platform.http` covers tenant creation, the public feature catalog, admin and user sign-in, tenant user creation, user context and profile, tenant feature updates, refresh, and logout. Change the sample tenant key and credentials before creating another tenant; tenant keys are unique. The feature update request replaces the tenant's selected features and initializes their database schemas when migrations are registered.
