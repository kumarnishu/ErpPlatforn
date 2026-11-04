# Shared authentication

This project defines shared claim names, JWT bearer validation, and HTTP-scoped `IUserContext` / `ITenantContext` implementations used by the APIs. Services can validate externally issued tokens with `Authentication:Authority` and `Authentication:Audience`, or locally issued tokens with `Authentication:Issuer`, `Authentication:Audience`, and a signing key; call `AddSharedJwtAuthentication()` and `AddSharedRequestContext()` to validate tokens and expose the resulting identity.

The Platform API issues tenant-scoped access tokens for its local tenant accounts. Other services should only accept validated bearer tokens. Never treat an unvalidated header or query parameter as the authenticated user or tenant.
