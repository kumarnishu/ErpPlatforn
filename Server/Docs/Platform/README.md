# Platform foundation: products, plans, subscriptions, and access

This document defines the core model for offering ERP, HRMS, Data Studio, and Checklists to multiple tenants. It is the product access foundation: it explains which products a tenant has, what each product plan enables, and which tenant users may enter those products.

The Platform service owns the shared catalog and access decisions. Each product service owns its own business data and enforces the access decision on every request.

> This is the intended design. Except for the Platform health endpoint, the model and endpoints below are not implemented yet.

## Keep four concepts separate

| Concept | Answers | Example |
| --- | --- | --- |
| **Product** | Which applications can be offered? | ERP, HRMS, Data Studio, Checklists |
| **Plan** | What does a product subscription include? | ERP Standard enables inventory and invoicing, with a user limit |
| **Subscription** | Which product plan does a tenant currently have? | Acme has active ERP Standard and Checklists Starter subscriptions |
| **Membership** | Which people belong to a tenant, and what can they do? | Sam is an Acme Tenantuser with access to Checklists |

A plan belongs to one product. A tenant can subscribe to multiple products, each with its own plan and lifecycle. A membership is between a user and a tenant; it is not a purchase or subscription.

```text
Product catalog
  └── Product plans (features, limits, optional price)

Tenant
  ├── Product subscriptions (one current subscription per product)
  └── User memberships
       └── Product access grants and product-specific permissions
```

## The three user roles

- **Superadmin**: platform-wide operator. Manages the product catalog, plans, tenant subscriptions, provisioning, and platform-wide administration.
- **Tenantadmin**: administers their tenant, including its memberships. Can see the tenant's subscriptions and entitlements. By default, can administer and enter every product that has an active subscription for that tenant.
- **Tenantuser**: uses only the subscribed products and features granted to them through their tenant membership. A Tenantuser has no Platform administration access.

Role and product access must always be scoped to a tenant. Being a Tenantadmin or Tenantuser in one tenant grants no access to another tenant. Do not turn **Tenantadmin**, **Tenantuser**, or **Superadmin** into product names or plans.

## Product access decision

Allow a user into a product only when all required conditions are true:

1. The tenant is active.
2. The user has an active membership in that tenant.
3. The tenant has an active subscription for that product, within its effective dates.
4. The subscription's plan enables the requested feature and has not exceeded applicable limits.
5. The membership role or product-specific grant permits the user to perform the requested action.

Tenantadmin receives tenant administration and access to all currently subscribed products by default. Tenantuser requires an explicit grant for each product (and appropriate permissions within it). A grant never enables an unsubscribed product; a subscription never grants an individual user access by itself.

For example, if Acme subscribes to ERP and Checklists, both products are available to Acme. Its Tenantadmin can administer Acme and use both products. A Tenantuser granted Checklists access can use Checklists but is denied ERP. HRMS and Data Studio remain unavailable until Acme subscribes to them, regardless of any user grant.

Pseudocode for a product request:

```text
allow = tenant.isActive
     && membership.isActive
     && subscription(tenant, product).isActive
     && subscription.plan.allows(feature)
     && membership.roleOrGrant.allows(action)
```

Superadmin's platform role authorizes platform administration; it should not silently bypass tenant-product checks in a product service. Any support or impersonation flow should be explicit, short-lived, permissioned, and audited.

## Core records

Use stable IDs or keys for products and features. Do not key authorization on display names, which can change.

- **Product**: `productId`, stable `key` (for example `erp`), display name, status, service identifier.
- **Plan**: `planId`, `productId`, name, status/version, entitlements, limits, optional billing metadata. A plan can only grant capabilities belonging to its product.
- **TenantProductSubscription**: `tenantId`, `productId`, `planId`, status, start/end dates, renewal/cancellation data, optional entitlement overrides. Enforce one effective current subscription for each tenant/product unless a deliberate migration policy says otherwise.
- **TenantMembership**: `tenantId`, `userId`, tenant role (`Tenantadmin` or `Tenantuser`), status, timestamps.
- **ProductAccessGrant**: `tenantId`, `userId`, `productId`, granted permissions/role, status, grantor and timestamps. Use this for Tenantuser access. Tenantadmin's default access can be derived from the role instead of inserting duplicate grants.
- **Feature definition**: stable feature key scoped to a product, type, and description. Plans supply enabled state and limits; tenant overrides are explicit and audited.

Store subscription status and dates separately from access grants. Use a clear state lifecycle such as `trialing`, `active`, `past_due`, `suspended`, `cancelled`, and `expired`; define which states permit access. The default should allow access only for `trialing` or `active` subscriptions whose effective date range includes the current time.

## Main lifecycle

### 1. Create tenant and select products

1. Superadmin creates a tenant.
2. Platform creates an initial Tenantadmin membership.
3. Superadmin assigns a plan for each product the tenant is purchasing; this creates a tenant-product subscription.
4. Provisioning prepares the product's resources and records the route/configuration needed by that product service.
5. Tenantadmin invites/adds people and assigns Tenantadmin or Tenantuser roles. For Tenantusers, Tenantadmin grants access to selected subscribed products.

Provisioning is asynchronous. A subscription can be pending setup; product access starts only when its status and provisioning policy say it is ready.

### 2. Add a product later

1. Superadmin or the authorized subscription workflow creates a subscription for the additional product.
2. Platform provisions it and activates access after successful setup.
3. Tenantadmin grants Tenantusers access to that product as needed.

Existing users do not automatically gain Tenantuser access merely because the tenant purchased another product.

### 3. Change plan, suspend, or cancel

- A plan change updates entitlements for that product only. Define whether the change is immediate or starts at renewal, and preserve the prior subscription state for audit/history.
- Suspending or cancelling a product subscription blocks that product for all users in that tenant while leaving other product subscriptions and memberships intact.
- Suspending the tenant blocks all tenant products.
- Removing or deactivating a membership blocks the user from every product in that tenant. Removing one product grant blocks only that product for that user.
- Revoke/update cached authorization promptly after membership, grant, subscription, tenant status, or plan entitlement changes.

## API areas to build

The detailed Platform endpoint proposal is in [Platform.md](Platform.md). The catalog and access model will need these endpoint areas:

- Product catalog and product-specific plan management (Superadmin).
- Tenant product subscription management and tenant subscription/entitlement reads.
- Tenant membership management and the authenticated user's tenant list.
- Tenantuser product grants, restricted to that tenant's Tenantadmins.
- Effective product entitlement/access lookup for trusted product services.
- Provisioning operations, audit events, and platform administration.

Consider a product-service endpoint or signed short-lived access token containing the tenant, user, product, effective feature entitlements, and authorization version. Product services must validate its issuer, audience, expiry, tenant, and product. Never trust tenant or role identifiers supplied only in a request body or URL; derive the caller identity from validated authentication and authorize the requested tenant against it.

## Security and consistency rules

- Every tenant-owned query and write is scoped by the authorized `tenantId`.
- Never trust a client-supplied tenant role, product grant, plan, or entitlement as proof of access.
- Product services enforce authorization server-side; hiding a product tile in a UI is not authorization.
- Tenantadmins cannot change subscriptions, plans, or platform configuration unless a separate explicit capability is added.
- Keep database credentials in a secret manager. Platform stores only secret references and routing metadata.
- Record who changed subscriptions, memberships, product grants, plans, and overrides. Never put credentials or tokens in audit records.
- Make retryable provisioning and subscription commands idempotent. Use optimistic concurrency when editing shared records.
- Document tenant data retention and offboarding separately from subscription cancellation; cancellation should not silently delete business data.

## Decisions to settle before implementation

1. Whether subscriptions are managed manually by Superadmin, through a billing provider, or both.
2. Whether Tenantadmin is always entitled to every subscribed product or can also be restricted by product grants. This document recommends automatic access for Tenantadmin and explicit per-product grants for Tenantuser.
3. The exact permission vocabulary inside each product (for example, `erp.invoices.read`), owned by that product team.
4. Trial, grace period, past-due, cancellation, and plan-change behavior.
5. How product services receive and refresh effective authorization, and the maximum acceptable cache/token lifetime for revocation.

These decisions should be captured as policy before implementing the corresponding endpoints so all four products enforce the same tenant and subscription rules.
