# Testing strategy

## Unit tests

Unit tests exercise domain invariants and URL policy without a database or network. They run quickly and are suitable for every local edit.

## Integration tests

Integration tests start PostgreSQL with Testcontainers, boot the real API composition root, apply EF migrations, and exercise registration, login, bearer authentication, monitor persistence, and ownership boundaries.

Run the suites with:

```bash
dotnet test Sentinel.slnx
```

Docker Desktop must be running for the integration suite. NuGet currently reports a high-severity advisory in Testcontainers' transitive SSH.NET dependency; this is test tooling only and remains a tracked dependency risk for the next package review.

