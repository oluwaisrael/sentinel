# Hetzner single-VM deployment

This package targets one Ubuntu VM with 2 vCPU and 4 GB RAM. It does not
provision a server, DNS record, domain, SMTP account, or firewall rule.

## Required host preparation

1. Create a non-root administrator with an SSH public key. Test a second SSH
   session before disabling password authentication.
2. Install Docker Engine and the Compose plugin from the vendor repository.
3. Copy `ops/sshd_config.d/sentinel.conf` into `/etc/ssh/sshd_config.d/`, run
   `sshd -t`, and reload SSH only after key login is confirmed.
4. Set `SSH_ADMIN_CIDR` to the office/VPN address range and run
   `ops/hetzner-firewall.sh`. Never use `0.0.0.0/0` for SSH.
5. Point the chosen DNS A/AAAA records at the VM and wait for propagation.

Only ports 80 and 443 should be publicly reachable. PostgreSQL, API, worker,
the Caddy admin endpoint, and Mailpit must remain private to the Compose
network.

## Configure and deploy

Copy `.env.production.example` to `.env.production` on the VM and replace all
placeholders using a secret manager or a root-readable file with mode 600. Do
not copy the file into Git.

Build the release images from the approved commit, then run the migration as a
separate step:

```bash
docker compose --env-file .env.production \
  -f docker-compose.yml -f docker-compose.production.yml \
  --profile migration build migrate
docker compose --env-file .env.production \
  -f docker-compose.yml -f docker-compose.production.yml \
  --profile migration run --rm migrate
docker compose --env-file .env.production \
  -f docker-compose.yml -f docker-compose.production.yml \
  build api worker web
docker compose --env-file .env.production \
  -f docker-compose.yml -f docker-compose.production.yml \
  up -d api worker web caddy
```

The production overlay removes public API, dashboard, and PostgreSQL ports.
Caddy terminates HTTPS, obtains certificates through ACME, and forwards to the
dashboard. Nginx forwards `/api/*` to the private API container, so the React
dashboard calls the same HTTPS origin without mixed content or production CORS
guesswork.

## Backups and restore

Run backups from a protected host directory and copy encrypted archives to
separate storage:

```bash
sudo install -d -m 700 /var/backups/sentinel
BACKUP_DIR=/var/backups/sentinel ./ops/backup-postgres.sh
sha256sum -c /var/backups/sentinel/sentinel-*.dump.sha256
```

For a restore, stop API and worker, verify the archive checksum, then run:

```bash
docker compose --env-file .env.production \
  -f docker-compose.yml -f docker-compose.production.yml stop api worker
CONFIRM_RESTORE=YES ./ops/restore-postgres.sh /var/backups/sentinel/sentinel-<timestamp>.dump
```

Run the migration step and the smoke test before restoring traffic. A backup is
not considered valid until a restore has been rehearsed on an isolated VM.

## Operations

- `docker compose ... ps` must show API healthy and worker running.
- Check `https://DOMAIN/health` and `https://DOMAIN/health/ready` through Caddy.
- Review `docker compose ... logs --since 15m api worker caddy`.
- Docker JSON logs are capped at 10 MB per file and five files per service.
- Resource limits are defined in the production overlay; watch memory pressure
  on the 4 GB VM before increasing concurrency.
- Configure Hetzner host-level monitoring and an external HTTPS check. Docker
  restart policies recover process crashes, but they do not replace alerting.
