# Platform API

This document describes the planned HTTP surface for the Platform service. These endpoints are a design list; apart from `GET /health`, they are not implemented yet. The Platform service owns the central tenant registry, tenant provisioning lifecycle, subscriptions, and service-to-database routing metadata. Business records remain owned by their respective services.

Base path: `/api/v1`

## User roles

- **Superadmin**: platform-wide access to tenant registry, plans, subscriptions, feature overrides, provisioning, platform-user administration, database-route administration, and audit logs.
- **Tenantadmin**: administrative access within assigned tenant(s), including tenant profile, tenant memberships, and viewing that tenant's subscription and effective features. Cannot administer other tenants or platform configuration.
- **Tenantuser**: access to the tenant features granted to them. Has no administrative access to Platform endpoints unless explicitly granted a separate permission.

Tenant roles come from the authenticated identity context and are scoped to a tenant. A user's tenant role must not grant access to another tenant. Platform-wide operations require the **Superadmin** role. Internal service endpoints require a trusted service identity and are not authorized by tenant-user roles.

## Health

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/health` | Liveness check. No authentication required. |

## Tenants

Superadmins manage all tenants. Tenantadmins can read and update only tenants they administer. Tenantusers cannot manage tenant records.

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/tenants` | List tenants visible to the caller. Platform admins can filter by `status`, `search`, and creation date. |
| `POST` | `/tenants` | Register a tenant and start its provisioning workflow. Returns `202 Accepted` with the tenant and provisioning operation IDs. |
| `GET` | `/tenants/{tenantId}` | Get tenant profile, status, and assigned plan. |
| `PATCH` | `/tenants/{tenantId}` | Update editable tenant profile fields. |
| `POST` | `/tenants/{tenantId}/suspend` | Suspend a tenant and prevent new tenant-scoped operations. |
| `POST` | `/tenants/{tenantId}/reactivate` | Reactivate a suspended tenant. |

## Tenant memberships

Tenantadmins manage memberships for their tenant; Superadmins can manage memberships across tenants. Tenantusers cannot manage memberships. Role changes and removals must invalidate affected user-context cache entries.

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/tenants/{tenantId}/memberships` | List members, with pagination and optional role/status filters. |
| `POST` | `/tenants/{tenantId}/memberships` | Add a user to a tenant and assign tenant roles. |
| `GET` | `/tenants/{tenantId}/memberships/{userId}` | Get one user's membership and roles. |
| `PATCH` | `/tenants/{tenantId}/memberships/{userId}` | Change tenant roles or membership status. |
| `DELETE` | `/tenants/{tenantId}/memberships/{userId}` | Remove a user's tenant membership. |
| `GET` | `/me/tenants` | List tenants the authenticated user can access. |

## Service database routes

Only trusted service identities can resolve routes. Route management is restricted to Superadmins and provisioning workflows. Tenantadmins and Tenantusers cannot manage or resolve database routes. Responses expose a secret reference, never a database password, credential, or connection string.

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/tenants/{tenantId}/database-routes` | List configured routes for the tenant, optionally filtered by `serviceId`. Admin only. |
| `PUT` | `/tenants/{tenantId}/database-routes/{serviceId}` | Create or replace a tenant/service route using a provider, database identifier, and secret reference. Admin only. |
| `DELETE` | `/tenants/{tenantId}/database-routes/{serviceId}` | Remove a tenant/service route. Admin only. |
| `POST` | `/internal/database-routes/resolve` | Resolve a route for `{ tenantId, serviceId }` for an authenticated service identity. Internal service-to-service endpoint. |

## Plans and subscriptions

Superadmins manage plans and subscription assignments. Tenantadmins can view their own tenant's subscription. Tenantusers cannot manage subscriptions.

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/plans` | List available plans and their feature/usage entitlements. |
| `POST` | `/plans` | Create a plan. Superadmin only. |
| `GET` | `/plans/{planId}` | Get plan details. |
| `PATCH` | `/plans/{planId}` | Update plan metadata or entitlements. Superadmin only. |
| `GET` | `/tenants/{tenantId}/subscription` | Get a tenant's current subscription and effective entitlements. |
| `PUT` | `/tenants/{tenantId}/subscription` | Assign or change a tenant's plan and subscription period. Superadmin only. |
| `POST` | `/tenants/{tenantId}/subscription/cancel` | Schedule or apply cancellation according to the subscription policy. Superadmin only. |

## Feature access

The Platform service owns tenant-level feature entitlements. Superadmins manage feature definitions and tenant overrides. Tenantadmins can view effective features for their own tenant; Tenantusers receive only the feature access granted by the tenant and their permissions. A business service remains responsible for enforcing its own feature gates using trusted platform data or a cache.

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/features` | List known platform feature definitions. Superadmin only. |
| `PUT` | `/tenants/{tenantId}/features/{featureKey}` | Override a tenant's feature state or limits. Superadmin only. |
| `DELETE` | `/tenants/{tenantId}/features/{featureKey}` | Remove a tenant override and return to plan defaults. Superadmin only. |
| `GET` | `/tenants/{tenantId}/features` | Get effective feature access after plan entitlements and overrides. Tenantadmin for their tenant, Superadmin for any tenant, or a trusted service. |

## Provisioning operations

Provisioning is asynchronous and restricted to Superadmins or trusted provisioning workflows. Tenantadmins and Tenantusers cannot start or retry provisioning. Clients can poll the operation resource and retry a failed operation where supported. Tenant creation and other retryable commands should accept an `Idempotency-Key` header.

| Method | Path | Purpose |
| --- | --- | --- |
| `POST` | `/tenants/{tenantId}/provisioning` | Start or retry provisioning tasks for an existing tenant. Superadmin or trusted provisioning identity. |
| `GET` | `/provisioning/{operationId}` | Get operation state, progress, timestamps, and safe error details. Superadmin only. |
| `GET` | `/tenants/{tenantId}/provisioning` | List provisioning operations for a tenant. Superadmin only. |
| `POST` | `/provisioning/{operationId}/retry` | Retry a retryable failed operation. Superadmin only. |

## Platform users

These are Superadmin accounts and roles, distinct from tenant memberships. Only Superadmins can administer platform users. Identity credentials remain with the configured identity provider.

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/platform-users` | List platform users. Superadmin only. |
| `POST` | `/platform-users` | Grant an existing identity platform access as a Superadmin. Superadmin only. |
| `GET` | `/platform-users/{userId}` | Get a platform user's roles and status. Superadmin only. |
| `PATCH` | `/platform-users/{userId}` | Update platform roles or status. Superadmin only. |
| `DELETE` | `/platform-users/{userId}` | Revoke Superadmin access without deleting the identity-provider account. Superadmin only. |
| `GET` | `/me` | Get the authenticated caller's profile, platform roles, and tenant memberships. Available to authenticated users. |

## Audit log

Audit records are append-only. Queries require Superadmin permissions and should be paginated and filterable by tenant, actor, action, and time range. Tenantadmins and Tenantusers cannot query platform audit logs. Sensitive secrets and credential values must never be recorded.

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/audit-logs` | Search platform audit events with pagination and filters. Superadmin only. |
| `GET` | `/audit-logs/{eventId}` | Get one audit event. Superadmin only. |

## API conventions

- Use JSON request/response bodies and the shared `ApiResponse<T>` / `ApiError` envelope.
- Use `401` for missing or invalid identity, `403` for insufficient permissions, `404` for missing resources, `409` for state conflicts, and `422` for validation failures.
- List endpoints use cursor pagination (`cursor`, `pageSize`) and return a continuation cursor.
- Use `ETag`/`If-Match` for updates where concurrent edits could overwrite state.
- Return `202 Accepted` plus an operation resource for long-running provisioning; do not keep HTTP requests open while infrastructure is being provisioned.
- Protect internal endpoints with service-to-service authentication and least-privilege service identities.
