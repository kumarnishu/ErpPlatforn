# Platform product access model

The Platform database currently models only product subscriptions and tenant-user permissions.

## Catalog

The platform offers ERP, Checklists, Data Studio, and HRMS. Each product contains modules; each module contains features; each feature contains actions/permissions such as `view`, `create`, `edit`, `export`, and `delete`. Product, module, feature, and action keys are stable identifiers. Catalog items can be activated or deactivated.

## Tenant subscriptions and user access

- Every tenant must have at least one active product subscription.
- Each tenant subscribes to products independently.
- A tenant user is represented by a membership scoped to that tenant.
- Actions are assigned to tenant memberships. Assignment to one tenant membership grants nothing in another tenant.
- A user can perform an action only when the tenant, membership, product subscription, product, module, feature, action, and that user's action assignment are active.

The minimum-one active subscription rule is enforced by tenant creation and subscription update workflows. The database has a unique tenant/product constraint and the persistence README describes the required authorization checks.

See [Platform-access-model.svg](Platform-access-model.svg) for the entity diagram and [Platform-endpoints.md](Platform-endpoints.md) for the implemented endpoint surface. The API expects a SQL Server connection string and JWT authority/audience settings.
