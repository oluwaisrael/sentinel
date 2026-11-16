# Sentinel architecture at Week 4

```text
React dashboard -> API -> Application contracts/services -> Infrastructure -> PostgreSQL
                                      ^                         ^
                                      |                         |
                              Domain invariants       Migration deployment job

Monitoring worker -> PostgreSQL lease -> HTTP target -> check result
Notification worker -> PostgreSQL outbox lease -> SMTP provider
```

The API and workers share application and infrastructure assemblies but remain separate hosts. PostgreSQL is the coordination boundary: monitor leases prevent concurrent checks, and outbox leases provide durable notification delivery without introducing Redis or a message broker.

## Retention policy

The intended default retention period is 90 days for check results, incident transitions, and notification outbox records. A future scheduled cleanup job should delete records older than the policy in batches, preserving aggregate monitor configuration and user accounts. Until that job is deployed, operators should monitor table growth and perform an approved maintenance operation rather than relying on unbounded history.

