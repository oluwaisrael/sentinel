# Sentinel

Sentinel is a production-minded API uptime monitoring and incident management platform built to demonstrate practical .NET backend engineering.

## Technology target

- C# / .NET 10
- ASP.NET Core Web API
- Entity Framework Core and PostgreSQL
- ASP.NET Core Identity
- BackgroundService for independent monitoring work
- xUnit and Testcontainers
- Docker and GitHub Actions
- Minimal React frontend (planned after the backend foundation)

## Four-week delivery plan

### Week 1 — Backend foundation

- Modular solution: API, Domain, Application, Infrastructure, Worker, UnitTests, IntegrationTests.
- PostgreSQL-backed persistence with EF Core migrations.
- ASP.NET Core Identity authentication and secure token-based API access.
- Monitor CRUD with ownership, validation, and pagination-ready contracts.
- Unit and integration test foundation.

### Week 2 — Monitoring engine

- Independent worker process using `BackgroundService`.
- Concurrency-safe scheduling and graceful cancellation.
- HTTP timeout handling and SSRF protections for registered endpoints.
- Durable check-result persistence and operational logging.

### Week 3 — Incidents and analytics

- Incident open/recovery state transitions.
- Recovery duration and uptime analytics.
- Durable email notification outbox with retry/backoff.
- API contracts and operational error handling.

### Week 4 — Delivery hardening

- Docker Compose development environment.
- GitHub Actions build/test pipeline.
- Minimal React dashboard for authentication, monitor management, and status.
- Deployment documentation, threat model, and production-readiness review.

## Engineering boundaries

Sentinel remains a modular monolith plus an independently deployable worker. Secrets are supplied through environment variables or deployment secret stores. No microservices, Kubernetes, or unnecessary AI features are in scope for the four-week build.

See [docs/IMPLEMENTATION_PLAN.md](docs/IMPLEMENTATION_PLAN.md) for the detailed milestone plan and acceptance criteria.
Architecture and security notes are in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) and [docs/SECURITY_MODEL.md](docs/SECURITY_MODEL.md).

## Local development

Use the pinned SDK in `global.json`. Provide `ConnectionStrings__DefaultConnection` and `Jwt__SigningKey` through your shell, a local secret store, or Docker Compose; production secrets are intentionally absent from repository configuration. Start Docker Desktop before running the integration suite.

```bash
dotnet build Sentinel.slnx
dotnet test Sentinel.slnx
```

