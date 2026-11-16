# Production smoke-test checklist

Run after DNS, TLS, migration, and service startup. Use a dedicated test
account and a public test endpoint that is safe to monitor.

## Edge and health

- [ ] `curl -I https://DOMAIN` returns HTTPS success and a valid certificate.
- [ ] HTTP redirects to HTTPS.
- [ ] `curl https://DOMAIN/health` returns 200.
- [ ] `curl https://DOMAIN/health/ready` returns 200.
- [ ] PostgreSQL is not reachable from the public internet.
- [ ] API, worker, and Caddy restart and return to healthy/running state.

## Authentication and authorization

- [ ] Register a test account and log in through the HTTPS dashboard.
- [ ] Create, edit, and delete a monitor.
- [ ] A second account cannot read or modify the first account's monitor,
  checks, statistics, or incidents.
- [ ] Invalid credentials and weak registration passwords are rejected.

## Monitoring and incidents

- [ ] Successful checks appear in history with status and latency.
- [ ] Simulate three failed checks and verify one open incident.
- [ ] Verify one outage notification is queued and delivered by SMTP.
- [ ] Simulate two successful checks and verify recovery plus one recovery
  notification.
- [ ] Confirm the same outage does not create duplicate open incidents/events.

## Security and operations

- [ ] Registering private, loopback, metadata, non-HTTP, and redirect-to-private
  destinations is rejected at registration or execution time.
- [ ] CORS allows only the configured HTTPS dashboard origin.
- [ ] SMTP credentials are present only in the deployment secret store/file.
- [ ] Backup creation, checksum verification, and isolated restore succeed.
- [ ] Migration history matches the approved release and was run before API
  and worker startup.
- [ ] Logs contain no JWT signing key, database password, or SMTP password.
