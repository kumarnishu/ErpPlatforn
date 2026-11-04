# Platform persistence

This context contains only the current access model:

- `Product -> ProductModule -> ProductFeature -> ProductAction`
- `Tenant -> TenantProductSubscription -> Product`
- `Tenant -> TenantMembership -> TenantUserAction -> ProductAction`

Tenant creation must create at least one active product subscription in the same registration workflow. Keep at least one active product subscription while the tenant is active. The schema enforces one row per tenant/product pair; the tenant creation and subscription update workflows must enforce the minimum-one rule.

Subscription changes update the tenant concurrency stamp. Concurrent attempts to remove the tenant's final active subscription result in `409 Conflict` rather than allowing both changes through.

To authorize a request, require an active tenant, active membership for the authenticated user in that tenant, an active subscription to the action's product, active product/module/feature/action catalog entries, and an active assignment for that membership and action. Product action assignments are tenant-scoped through `TenantMembership`.

The Platform host currently uses SQL Server. Configure `ConnectionStrings:PlatformDatabase` (or `PLATFORM_DB_CONNECTION`) and apply the migration with:

```powershell
dotnet ef database update --project Server/Services/Platform/Platform.Infrastructure/Platform.Infrastructure.csproj --startup-project Server/Services/Platform/Platform.Api/Platform.Api.csproj
```

All IDs are `int`; enum values are stored as readable strings.
