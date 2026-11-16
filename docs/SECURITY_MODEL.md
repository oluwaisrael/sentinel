# Week 1 security model

Sentinel treats monitor URLs as user-controlled outbound-request policy. The API performs cheap deterministic checks before persistence, while the Week 2 worker will repeat the checks immediately before connecting.

Identity uses ASP.NET Core Identity's password hasher, unique email enforcement, lockout, and token-provider infrastructure. JWT signing keys are configuration-only and are rejected at startup when shorter than 32 characters.

Every monitor query includes the authenticated owner ID. This makes ownership part of the data-access predicate and prevents an ID-only controller lookup from becoming an authorization bypass.

The current scope does not include admin roles, refresh tokens, password reset email, or multi-region key rotation. Those are explicit follow-up decisions rather than accidental behavior.

The integration test dependency warning was remediated by explicitly resolving SSH.NET to 2026.0.0, the patched release for the Testcontainers transitive dependency. The remaining dependency audit should be rerun in CI as part of Week 4 hardening.

SMTP credentials are configuration-only. Notification event IDs are stable across retries, but SMTP delivery is at-least-once and may duplicate if a crash occurs after provider acceptance and before the outbox status update.

