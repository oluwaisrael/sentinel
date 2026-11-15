# Sentinel implementation plan

## Architecture

The solution uses pragmatic Clean Architecture boundaries:

- **Domain** contains entities, value objects, and domain rules with no infrastructure dependencies.
- **Application** contains use cases, DTOs, validation, and ports/interfaces.
- **Infrastructure** contains EF Core, PostgreSQL, Identity, persistence adapters, and external integrations.
- **API** contains HTTP endpoints, authentication configuration, middleware, and composition root concerns.
- **Worker** is an independent host that reuses Application and Infrastructure to execute checks.
- **UnitTests** verify domain/application behavior without external services.
- **IntegrationTests** verify the real PostgreSQL and HTTP composition using Testcontainers.

## Week 1 acceptance criteria

1. `dotnet build` succeeds using the pinned .NET 10 SDK.
2. The API starts with PostgreSQL configuration supplied by environment variables.
3. Users can register and authenticate without secrets committed to source control.
4. Authenticated users can create, read, update, and delete only their own monitors.
5. Monitor URLs, names, intervals, and timeouts are validated at the application boundary.
6. EF Core migrations create the required PostgreSQL schema.
7. Unit tests cover monitor validation and ownership behavior.
8. Integration tests cover registration, login, and monitor CRUD against PostgreSQL.

## Security decisions

- Passwords are delegated to ASP.NET Core Identity's hardened password hashing and lockout primitives.
- JWT signing material is configuration-only and must be provided outside source control.
- Monitor ownership is enforced in application queries and mutation handlers, not just controller code.
- URL validation will be centralized so the Week 2 worker can add DNS/IP SSRF checks without duplicating policy.
- API errors use consistent problem details responses and avoid leaking infrastructure details.

## Deliberate tradeoffs

- A modular monolith keeps deployment and debugging realistic for a four-week portfolio project while preserving clear boundaries.
- PostgreSQL is the source of truth; background work will use durable rows rather than an in-memory queue.
- The initial API favors straightforward REST endpoints and explicit DTOs over generic repositories or CQRS framework overhead.

