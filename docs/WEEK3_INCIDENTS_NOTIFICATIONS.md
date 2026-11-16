# Week 3 incidents and notifications

## Incident policy

- A monitor opens an incident after three consecutive unhealthy checks.
- A monitor recovers an open incident after two consecutive healthy checks.
- A prolonged outage produces one incident and one outage notification, not one event per failed check.
- After recovery, a later three-failure sequence opens a new incident episode.

`Monitor` stores consecutive counters so the threshold decision is transactional with the check result. `IncidentTransition` records the opened and recovered transitions and points to the triggering check result.

## Database guarantees

The `incidents` table has a PostgreSQL partial unique index for open incidents:

```sql
UNIQUE ("MonitorId") WHERE "Status" = 'Open'
```

The check result, monitor counters, incident transition, incident state, and notification outbox row are committed in one transaction. Deleting a monitor cascades to incidents, transitions, check results, and outbox records.

The outbox has a unique `(IncidentId, EventType)` index. Its outbox ID is also the stable logical event ID.

## Notification delivery

Notification workers claim pending, failed-due, or expired-processing rows using PostgreSQL leases and `SKIP LOCKED`. Attempts are bounded by `Notifications:MaxAttempts`. Retry delay is exponential, starting at `Notifications:RetryBaseDelaySeconds`, capped at one hour.

Delivery is at-least-once. If the process sends an email and crashes before marking the row sent, the email may be sent again after lease expiry. The stable event ID is supplied to the provider as `IncidentNotification.EventId`; providers that support idempotency should use it as their idempotency key. SMTP itself does not provide universal idempotency, so duplicate delivery remains possible.

Notification delivery runs in a separate bounded worker. Provider failures are persisted and retried without blocking monitoring checks.

## Configuration

```text
Notifications__MaxConcurrency=2
Notifications__MaxAttempts=5
Notifications__RetryBaseDelaySeconds=30
Email__Host=<smtp-host>
Email__Port=587
Email__Username=<secret-user>
Email__Password=<secret-password>
Email__From=alerts@example.com
Email__UseSsl=true
```

Email credentials must come from environment variables or a deployment secret store.

## API

`GET /api/monitors/{id}/incidents?from=<iso>&to=<iso>&limit=100` returns owner-scoped incident history. The window is bounded to 31 days and the limit to 500.
