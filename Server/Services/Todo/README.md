# Todo service

This is an independently deployable Todo microservice created from `ServiceTemplate`. Its stable service ID is `todo`.

The service resolves a tenant database route with `(tenantId, "todo")`, then retrieves credentials through the route's secret reference and creates its own DbContext. Todo data and schema migrations belong to this service; they do not belong in the shared libraries.

The domain contains a user-owned Todo item and the application layer defines its repository contract. The API currently exposes the template health endpoint; CRUD endpoints and provider-specific persistence should be added after choosing the database provider and connecting the tenant route resolver.
