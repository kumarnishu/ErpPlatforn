# Microservice template

Copy this folder to create an independently deployable service such as Employees, Products, Orders, Inventory, or Customers. Give it a stable service ID and its own database context, schema migrations, API, application use cases, domain rules, and infrastructure integrations.

The service resolves a tenant database route with `(tenantId, serviceId)`, then asks its infrastructure adapter to retrieve credentials through the route's secret reference and create that service's DbContext. The schema and migrations remain owned by this service.
