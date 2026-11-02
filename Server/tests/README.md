# Automated server tests

Automated tests live here, grouped by service. For example, `Services/Todo/Todo.Domain.Tests` verifies Todo domain behavior. Add service application tests and database/API integration tests under the owning service folder as those components are implemented.

Run all automated server tests from `Server/` with `dotnet test Server.slnx`.

Manual HTTP requests live separately in `Server/manual-tests/`; keep `.http` files there rather than beside service code.
