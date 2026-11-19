# Shared JWT

This separate building block registers JWT bearer authentication and authorization for API projects. `JwtClaimTypes` defines the claim names expected in validated tokens.

Call `AddSharedJwtAuthentication()` to validate JWTs issued by this application. Configure `Authentication:Issuer`, `Authentication:Audience`, and `Authentication:SigningKey`; the APIs validate the tokens your application issues using these values.

