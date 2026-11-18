# Shared JWT

This separate building block registers JWT bearer authentication and authorization for API projects. `JwtClaimTypes` defines the claim names expected in validated tokens.

Call `AddSharedJwtAuthentication()` to configure either an external issuer with `Authentication:Authority` and `Authentication:Audience`, or a local signing key with `Authentication:Issuer`, `Authentication:Audience`, and `Authentication:SigningKey`.
