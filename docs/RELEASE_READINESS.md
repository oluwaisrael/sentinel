# Release-readiness review

Review date: 2026-11-16

## Verified

- GitHub Actions for `e0eac3c` completed successfully: restore, build, tests,
  dependency audit, and frontend build.
- Browser smoke test passed with Google Chrome and Playwright: registration,
  login, monitor create/edit/delete, bounded check and incident history views,
  sign-out, and login again.
- PostgreSQL integration tests passed in isolation: 16 tests, including owner
  authorization, concurrent claims, lease recovery, SSRF rejection, incident
  transitions, deduplication, and notification retry behavior.
- Unit tests passed: 36 tests.
- A clean Compose environment applied migrations, started API and worker, and
  returned HTTP 200 from `/health/ready`.
- Mailpit captured one real worker-delivered outage email. The outbox row was
  marked `Sent` after one attempt. No external recipient was used.
- Compose validation passed with required secrets supplied through environment
  variables. PostgreSQL is bound to loopback only.
- Repository scans found no committed private-key, cloud-access-key, or GitHub
  token patterns. `.env` is ignored and only `.env.example` is tracked.

## Important findings fixed during review

- The dashboard image used its baked-in API URL; the E2E stack now supplies the
  matching URL at build time and the test asserts the real dashboard elements.
- A named Data Protection volume is mounted after image creation, so an image
  layer `chown` alone is insufficient. The API entrypoint now initializes the
  mounted directory as root and immediately drops to the `app` user.
- Integration tests can time out if run while another Compose PostgreSQL stack
  is consuming Docker resources. Re-running after removing the disposable
  stack passed all 16 tests.

## Untested assumptions and remaining risks

- No public deployment was performed, so TLS termination, DNS, backups,
  firewall rules, provider-specific SMTP behavior, and restart recovery on a
  real VM remain unverified.
- SMTP delivery is at-least-once. A provider accepting a message before a
  worker crash can still result in duplicate delivery.
- The current retention policy is documented but automated cleanup is not yet
  implemented.
- The release browser test validates empty history states. Incident data and
  recovery rendering are covered by PostgreSQL integration tests, not by the
  browser flow.
- Docker image rebuilds were not completed during this review because a
  transient NuGet CDN download timed out. Local .NET build and tests passed;
  the new API entrypoint should be rebuilt in CI before deployment.

## Deployment gate

Do not deploy until a CI run for the final commit has passed and the final API,
worker, migration, and dashboard images have been built from that commit. Run
the migration image as a separate one-shot deployment step, then start API and
worker with production secrets injected by the host.
