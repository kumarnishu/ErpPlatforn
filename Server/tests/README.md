# Automated server tests

Automated tests live here, grouped by service. `Services/Platform/Platform.Application.Tests` covers Platform service rules using an in-memory repository fake; it does not require SQL Server.

Run all automated server tests from `Server/` with `dotnet test Server.slnx`.

Manual HTTP requests live separately in `Server/manual-tests/`; `Platform-Access.http` exercises the Platform catalog, signup, subscriptions, memberships, action grants, and authorization checks. It needs a configured Platform database and JWT tokens.
