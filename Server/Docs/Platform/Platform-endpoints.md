# Platform API endpoints

This document defines the Platform API surface for the current entities: `PlatformUser`, `Tenant`, `Product`, `ProductModule`, `ProductFeature`, `ProductAction`, `TenantProductSubscription`, `TenantMembership`, and `TenantUserAction`.

Apart from `GET /health`, the endpoints below are implemented in `Platform.Api`. Base path: `/api/v1`.

Configure `ConnectionStrings:PlatformDatabase` or the `PLATFORM_DB_CONNECTION` environment variable with a SQL Server connection string, then apply the EF migration before using data routes. Without a connection string, the API keeps `/health` available and returns `503 Service Unavailable` for `/api/v1/*`.

For local tenant accounts, configure `Authentication:Issuer`, `Authentication:Audience`, and `Authentication:SigningKey` (at least 32 random bytes, preferably base64 encoded). Keep the signing key in an environment variable or secret store. The API issues short-lived tenant-scoped JWTs and stores salted PBKDF2 password hashes. Alternatively, configure both `Authentication:Authority` and `Authentication:Audience` for externally issued tokens. Do not configure local signing key and external authority together. Tenant account login requires the local signing key. Platform catalog writes require a `PlatformAdmin` role claim. User access checks use the authenticated subject, never a request-supplied user ID.

## Rules

- Tenant signup must select at least one active product. Create the tenant, local user account, `TenantAdmin` membership, and initial active product subscription(s) in one database save.
- A tenant may subscribe to multiple products, with one subscription record per tenant/product pair.
- Do not cancel or suspend a tenant's final active product subscription while that tenant remains active. Return `409 Conflict` if the request would leave the tenant with no active product.
- Tenant memberships are scoped by `TenantId` and have either the `TenantAdmin` or `TenantUser` role. Local account IDs are stable platform subjects.
- A user may perform an action only if the tenant, membership, product subscription, product, module, feature, action, and exact user-action assignment are all active.
- Catalog and subscription administration requires an authenticated `PlatformAdmin` role claim. Tenant user creation requires an active `TenantAdmin` membership for the tenant in the route. Never treat a route tenant ID as proof of membership.
- Use `PATCH` to deactivate catalog records or revoke memberships/assignments. Avoid hard deletion of records referenced by subscriptions or assignments.

## Health

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/health` | Check that the Platform API is running. |

## Current authenticated user

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/auth/me` | Validate the bearer token and return its authenticated user ID and roles. |

## Tenant account authentication

Registration accepts one or more of the four seeded product IDs (ERP `1`, Checklist `2`, Data Studio `3`, HRMS `4`). Login requires a tenant ID because one email account can belong to multiple tenants; the returned JWT is scoped to the selected active membership and role.

| Method | Path | Purpose |
| --- | --- | --- |
| `POST` | `/auth/register-tenant-admin` | Publicly create a tenant, first tenant-admin account, membership, and selected product subscriptions. |
| `POST` | `/auth/login` | Verify email/password and active membership, then issue a tenant-scoped JWT. |

Registration request:

```json
{
  "tenantKey": "acme",
  "tenantName": "Acme Ltd",
  "displayName": "Avery Admin",
  "email": "admin@acme.example",
  "password": "a-long-password-at-least-12-chars",
  "productIds": [1]
}
```

Login request:

```json
{ "email": "admin@acme.example", "password": "a-long-password-at-least-12-chars", "tenantId": 1 }
```

## Products

Manage the top-level products: ERP, Checklists, Data Studio, and HRMS. `key` is a stable identifier; it must be unique. Product status is `Active` or `Inactive`.

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/products` | List products. Supports `status`, `page`, and `pageSize` filters. |
| `POST` | `/products` | Create a product with `key` and `name`; it starts active. |
| `GET` | `/products/{productId}` | Get a product. |
| `PATCH` | `/products/{productId}` | Update the name or status. Product key is immutable after creation. |

## Product modules

Modules are always scoped to a product. A module key must be unique within its product.

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/products/{productId}/modules` | List the product's modules. Supports `status`, `page`, and `pageSize`. |
| `POST` | `/products/{productId}/modules` | Create an active module with `key`, `name`, and optional `sortOrder`. |
| `GET` | `/products/{productId}/modules/{moduleId}` | Get a module. |
| `PATCH` | `/products/{productId}/modules/{moduleId}` | Update module name, sort order, or status. Module key is immutable. |

## Product features

Features are always scoped to a module. A feature key must be unique within its module.

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/products/{productId}/modules/{moduleId}/features` | List a module's features. Supports `status`, `page`, and `pageSize`. |
| `POST` | `/products/{productId}/modules/{moduleId}/features` | Create an active feature with `key`, `name`, and optional `sortOrder`. |
| `GET` | `/products/{productId}/modules/{moduleId}/features/{featureId}` | Get a feature. |
| `PATCH` | `/products/{productId}/modules/{moduleId}/features/{featureId}` | Update feature name, sort order, or status. Feature key is immutable. |

## Feature actions / permissions

Actions are always scoped to a feature. Examples include `view`, `create`, `edit`, `export`, and `delete`. Action keys are data, so each feature can define only the permissions it needs. An action key must be unique within its feature.

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/products/{productId}/modules/{moduleId}/features/{featureId}/actions` | List a feature's actions. Supports `status`, `page`, and `pageSize`. |
| `POST` | `/products/{productId}/modules/{moduleId}/features/{featureId}/actions` | Create an active action with `key` and `name`. |
| `GET` | `/products/{productId}/modules/{moduleId}/features/{featureId}/actions/{actionId}` | Get an action. |
| `PATCH` | `/products/{productId}/modules/{moduleId}/features/{featureId}/actions/{actionId}` | Update action name or status. Action key is immutable. |

## Tenants and signup

The public signup route is `/auth/register-tenant-admin`; validate distinct active product IDs and return `400 Bad Request` for an empty selection.

| Method | Path | Purpose |
| --- | --- | --- |
| `POST` | `/auth/register-tenant-admin` | Create the tenant, first tenant admin, and initial product subscription(s). |
| `GET` | `/tenants` | List tenants visible to the caller. Supports `status`, `search`, `page`, and `pageSize`. |
| `GET` | `/tenants/{tenantId}` | Get a tenant and its current active product subscriptions. |
| `PATCH` | `/tenants/{tenantId}` | Update tenant name or status (`Active` / `Suspended`). |
| `GET` | `/me/tenants` | List tenants where the authenticated user has an active membership, with their active product subscriptions. |

Tenant keys are unique. A suspended tenant cannot access any product, even when its subscriptions and user assignments remain active.

## Tenant product subscriptions

Subscriptions are direct tenant-to-product relationships; there are no plans or billing entities in the current model. The unique key is `(tenantId, productId)`.

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/tenants/{tenantId}/product-subscriptions` | List the tenant's product subscriptions. Supports `status`, `page`, and `pageSize`. |
| `PUT` | `/tenants/{tenantId}/product-subscriptions/{productId}` | Subscribe to a product or reactivate its existing subscription. Idempotent for an already active subscription. |
| `GET` | `/tenants/{tenantId}/product-subscriptions/{productId}` | Get the tenant's subscription to one product. |
| `PATCH` | `/tenants/{tenantId}/product-subscriptions/{productId}` | Change subscription status or expiration. Do not leave an active tenant with zero active products. |

Only active products may be newly subscribed to. Deactivating a product should block new subscriptions; it must not silently remove tenant subscriptions.

## Tenant memberships

A membership links a local platform `userId` to one tenant and has a `TenantAdmin` or `TenantUser` role. The unique key is `(tenantId, userId)`.

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/tenants/{tenantId}/memberships` | List tenant memberships. Supports `status`, `search`, `page`, and `pageSize`. |
| `POST` | `/tenants/{tenantId}/users` | Tenant admin creates a local tenant user with email and password. The new member starts with no action grants. |
| `POST` | `/tenants/{tenantId}/memberships` | Platform admin links an existing external identity by its user ID. New memberships start as `TenantUser`. |
| `GET` | `/tenants/{tenantId}/memberships/{membershipId}` | Get a membership and its assigned action permissions. |
| `PATCH` | `/tenants/{tenantId}/memberships/{membershipId}` | Activate or revoke the membership. Revoking a membership immediately blocks all its action assignments. |

## Tenant user action assignments

Assignments link a `TenantMembership` to a `ProductAction`. They grant only the exact action on the exact feature. Before assigning an action, verify the tenant has an active subscription to the product containing that action.

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/tenants/{tenantId}/memberships/{membershipId}/actions` | List actions assigned to the membership. Supports `productId`, `isActive`, `page`, and `pageSize`. |
| `PUT` | `/tenants/{tenantId}/memberships/{membershipId}/actions/{actionId}` | Assign or reactivate the action. Idempotent for an already active assignment. |
| `DELETE` | `/tenants/{tenantId}/memberships/{membershipId}/actions/{actionId}` | Revoke the assignment by setting `IsActive` to `false`; retain the record. |
| `GET` | `/tenants/{tenantId}/me/actions` | List the authenticated user's effective active actions for the tenant. Supports `productId`, `page`, and `pageSize`. |

Return `404` when the membership or action is outside the requested tenant/product scope. Return `409 Conflict` if the tenant has no active subscription to the action's product.

## Authorization check

Product services need an authoritative answer for the authenticated user and requested product action. Derive the user subject from a validated user token or trusted service identity; do not accept an arbitrary `userId` as authorization proof.

| Method | Path | Purpose |
| --- | --- | --- |
| `POST` | `/tenants/{tenantId}/authorization/check` | Check whether the authenticated tenant user may perform an action. |

Example request:

```json
{
  "productKey": "erp",
  "moduleKey": "invoices",
  "featureKey": "invoice-records",
  "actionKey": "export"
}
```

Return `200 OK` with `{ "allowed": true }` when every required record and the exact assignment are active. Return `{ "allowed": false }` with a stable denial code such as `tenant_inactive`, `membership_inactive`, `product_not_subscribed`, `catalog_item_inactive`, or `action_not_assigned` for a valid request that fails authorization. Use `400 Bad Request` for a malformed request and `404 Not Found` when the requested catalog path does not exist.

## Consistent authorization predicate

For a requested `(tenantId, authenticatedUserId, productKey, moduleKey, featureKey, actionKey)`, allow only when all of the following are true:

```text
tenant.status == Active
AND membership(tenantId, authenticatedUserId).status == Active
AND tenantProductSubscription(tenantId, productId).status == Active
AND product.status == Active
AND module.status == Active
AND feature.status == Active
AND action.status == Active
AND tenantUserAction(membershipId, actionId).isActive == true
```

The tenant subscription, membership, and action assignment must all refer to the same tenant and the requested product action. A UI hiding a feature is not authorization; product services must enforce this predicate on every protected request.
