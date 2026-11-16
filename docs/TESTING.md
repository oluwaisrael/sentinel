# Testing strategy

## Unit tests

Unit tests exercise domain invariants and URL policy without a database or network. They run quickly and are suitable for every local edit.

## Integration tests

Integration tests start PostgreSQL with Testcontainers, boot the real API composition root, apply EF migrations, and exercise registration, login, bearer authentication, monitor persistence, and ownership boundaries.

Run the suites with:

```bash
dotnet test Sentinel.slnx
```

Docker Desktop must be running for the integration suite. The Testcontainers SSH.NET advisory is remediated by the explicit SSH.NET 2026.0.0 override; CI also runs NuGet audit mode with a high-severity failure threshold.

To create a new migration, set `SENTINEL_CONNECTION_STRING` in the shell and run `dotnet ef migrations add <Name>` against the Infrastructure project. The design-time factory intentionally fails when that variable is absent.

