# Week 2 monitoring engine

## Scheduling and delivery semantics

The worker uses a PostgreSQL-backed lease. Each claim atomically selects one due, enabled monitor with a supported 60-second interval, skips rows already locked by another worker, assigns a unique lease ID, and sets an expiry timestamp.

The worker runs a fixed number of processing loops (`Monitoring:MaxConcurrency`, default `4`). Each loop claims at most one monitor at a time, so task creation is bounded. Results are committed in one transaction with the check result and the next schedule time. Completion verifies the lease ID, so a late worker cannot overwrite a monitor after its lease has been reclaimed.

This is at-least-once scheduling. A process crash after the HTTP request but before the transaction commits leaves the lease to expire; another worker retries the check. That can produce duplicate outbound checks in a failure window, but it cannot produce duplicate committed results for the same lease token.

## HTTP security and behavior

- Requests use the named `IHttpClientFactory` client `monitor-checks`.
- Redirects are disabled; 3xx responses are recorded as non-healthy rather than followed.
- The checker resolves and validates the destination at execution time.
- The socket connect callback resolves again immediately before connecting and only dials public addresses.
- HTTP and HTTPS are the only allowed schemes.
- The request timeout is capped at five seconds; shorter monitor timeouts are honored.
- Healthy responses are status codes 200–299.
- DNS failures, restricted destinations, connection failures, timeouts, cancellations, and unexpected errors are persisted as typed `ErrorType` values.

## Worker configuration

The worker reads these settings from configuration or environment variables:

```text
ConnectionStrings__DefaultConnection
Monitoring__PollIntervalMilliseconds=1000
Monitoring__MaxConcurrency=4
Monitoring__LeaseDurationSeconds=30
Monitoring__SupportedIntervalSeconds=60
```

Run the independent worker host with:

```bash
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=sentinel;Username=sentinel;Password=<secret>'
export Jwt__Issuer='sentinel-api'
export Jwt__Audience='sentinel-client'
export Jwt__SigningKey='<at-least-32-character-secret>'
dotnet run --project src/Sentinel.Worker/Sentinel.Worker.csproj
```

The worker applies pending EF migrations on startup and then begins polling. It can run separately from the API process against the same PostgreSQL database.

## Query bounds

`GET /api/monitors/{id}/checks` accepts `from`, `to`, and `limit`; the limit is 1–500 and the time window is capped at 31 days. `GET /api/monitors/{id}/stats` uses the same maximum window and defaults to the last seven days.

