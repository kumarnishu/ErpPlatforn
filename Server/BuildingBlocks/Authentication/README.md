# Shared authentication

This project defines the shared claim names and HTTP-scoped `IUserContext` / `ITenantContext` implementations used by every API. Each service should validate the same issuer, audience, signing keys, and token rules, then call `AddSharedRequestContext()` to expose the validated identity.

The identity provider and token-validation package are intentionally not selected here. Add that provider configuration once in this shared project when the platform's identity provider is chosen. Never treat an unvalidated header or query parameter as the authenticated user or tenant.
