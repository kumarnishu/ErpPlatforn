# Manual API requests

These `.http` files can be run with the VS Code REST Client extension or another HTTP client that supports the `.http` request format.

Start the APIs with Docker Compose from `docker/` or run the projects directly. The default local URLs are Platform API `http://localhost:5000` and Todos API `http://localhost:5001`.

The Platform and Todos APIs require a configured PostgreSQL connection and their migrations applied. Set `Authentication__Issuer`, `Authentication__Audience`, and `Authentication__SigningKey` for login and authenticated routes. Both APIs keep their development session cache in memory, so a Platform-issued token will not validate in Todos across separate API processes unless both are configured with a shared cache provider.

`Platform.http` creates a tenant and its initial admin, signs in, creates a tenant user, refreshes tokens, reads user context, and signs out. Change the sample tenant key and credentials before creating another tenant; tenant keys are unique.
