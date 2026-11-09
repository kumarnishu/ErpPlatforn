# Shared authentication

This project defines shared claim names, JWT bearer validation, and HTTP-scoped `IUserContext` / `ITenantContext` implementations used by the APIs. Services can validate externally issued tokens with `Authentication:Authority` and `Authentication:Audience`, or locally issued tokens with `Authentication:Issuer`, `Authentication:Audience`, and a signing key; call `AddSharedJwtAuthentication()` and `AddSharedRequestContext()` to validate tokens and expose the resulting identity.

Service hosts should accept only validated bearer tokens. Never treat an unvalidated header or query parameter as the authenticated user or tenant. Configure each host with the identity provider's authority and audience.
