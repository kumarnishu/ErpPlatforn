# User context

This library defines the simple `UserContext` model for login data used to issue a JWT. It contains UUID user, tenant, and session IDs; tenant key; email; user and tenant active flags; a boolean token version; role; and subscription expiry. Tenant keys must be between 2 and 4 characters.

Register request access with `AddUserContext()` and the cache with `AddSharedDistributedCaches()`. On successful login, insert `new UserToken(userContext.UserId, userContext.TenantId, userContext.SessionId, userContext.TokenVersion)` with `IUserTokenCache.TryAddAsync` before returning the JWT. Add `app.UseMiddleware<GlobalExceptionMiddleware>()` before authentication, then add `app.UseAuthentication()`, `app.UseMiddleware<UserContextMiddleware>()`, and `app.UseAuthorization()` in that order. The user middleware extracts the user, tenant, role, status, token version, session, and subscription expiry claims from the validated JWT, checks the cached token for that session, and sets `IUserContextAccessor.Current` for the request. A missing cache entry or mismatched user, tenant, or token version throws an API 401 exception.

After login, use `UserContext` to create the JWT. Save only `UserId`, `TenantId`, `SessionId`, and `TokenVersion` as a `UserToken` in the cache. `IUserTokenCache.TryAddAsync` returns `false` if that session ID is already cached.
