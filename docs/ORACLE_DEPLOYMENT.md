# Oracle Cloud Always Free deployment

This runbook targets one Ubuntu VM using `VM.Standard.A1.Flex` (Ampere ARM64)
with 2 OCPUs and 12 GB RAM. Oracle's Always Free A1 allocation is shared by
the tenancy, and the free allowance is limited to 1,500 OCPU-hours and 9,000
GB-hours per month. Confirm current eligibility and capacity in Oracle's
[Always Free resources documentation](https://docs.oracle.com/en-us/iaas/Content/FreeTier/freetier_topic-Always_Free_Resources.htm).

No account upgrade, VM, VCN, public IP, DNS record, or other cloud resource is
created by this repository.

## Always Free resource plan

Use only these resources:

- One Ampere A1 ARM64 VM: 2 OCPUs and 12 GB RAM.
- One boot volume and a modest block volume within the Always Free storage
  allowance; keep PostgreSQL data and backups within the current tenancy limit.
- One public IPv4 address for the VM. The public address is needed for SSH,
  ACME HTTP/TLS validation, and HTTPS traffic.
- One VCN, public subnet, internet gateway, and route table. Do not add a paid
  load balancer, NAT gateway, managed database, or paid monitoring service.

Always Free capacity is not guaranteed. An `out of host capacity` error means
the requested shape is temporarily unavailable in the selected home region or
availability domain; wait or try another availability domain in the home
region. Do not upgrade the tenancy just to bypass capacity without explicit
approval.

Oracle may reclaim an idle Always Free compute instance. Oracle documents the
idle condition as CPU, network, and A1 memory utilization each below 20% at the
95th percentile over seven days. A monitoring service may naturally be low
traffic, so treat reclamation as a platform risk and keep backups outside the
VM.

## VCN and public IPv4 rules

Create a public subnet only because this single VM needs inbound HTTPS and
ACME validation. The subnet route table needs an internet gateway route for
`0.0.0.0/0`; the VM needs an ephemeral or reserved public IPv4 address.

Configure the VCN security list or, preferably, a network security group with
these stateful ingress rules:

| Source | Protocol | Port | Purpose |
|---|---|---:|---|
| `SSH_ADMIN_CIDR` only | TCP | 22 | Restricted administration |
| `0.0.0.0/0` | TCP | 80 | ACME validation and HTTP redirect |
| `0.0.0.0/0` | TCP | 443 | HTTPS application traffic |

Do not allow 5432, 8080, 3000, 8025, or 2019 from any source. Oracle also
recommends restricting SSH to authorized CIDRs rather than opening port 22 to
`0.0.0.0/0`; see [OCI networking security guidance](https://docs.oracle.com/en-us/iaas/Content/Security/Reference/networking_security.htm).

## Host security

1. Create a non-root Ubuntu administrator and verify key-based SSH from a
   second terminal before changing SSH settings.
2. Install Docker Engine and the Compose plugin for ARM64.
3. Copy `ops/sshd_config.d/sentinel.conf` into `/etc/ssh/sshd_config.d/`, run
   `sshd -t`, and reload SSH only after key login is confirmed.
4. Set `SSH_ADMIN_CIDR` and run `ops/ubuntu-firewall.sh`.
5. Keep Ubuntu security updates enabled and do not expose Docker's daemon
   socket or TCP API.

The OCI security rules and UFW rules are defense in depth. Both layers must
allow 80/443 and both must restrict 22.

## DuckDNS and HTTPS

Create a free DuckDNS hostname manually, for example
`sentinel-yourname.duckdns.org`, and point it to the VM's public IPv4 address.
Do not put a DuckDNS token in Git; update the record from a protected local
machine or a secret-backed updater only after approval.

Set `DOMAIN` to the DuckDNS hostname and `ACME_EMAIL` to an address you control.
Caddy obtains and renews the certificate automatically. The React image is
built with an empty `VITE_API_BASE_URL`; browser requests use `/api/...` on the
same HTTPS origin. Nginx forwards that path privately to the API container.

## Deploy from an approved commit

Copy `.env.production.example` to `.env.production` on the VM, set mode 600,
and inject real secrets. Then build the ARM64 images and run migrations as a
separate step:

```bash
docker compose --env-file .env.production \
  -f docker-compose.yml -f docker-compose.production.yml build api worker web migrate
docker compose --env-file .env.production \
  -f docker-compose.yml -f docker-compose.production.yml \
  --profile migration run --rm migrate
docker compose --env-file .env.production \
  -f docker-compose.yml -f docker-compose.production.yml \
  up -d api worker web caddy
```

The production overlay removes public PostgreSQL, API, and dashboard ports.
Only Caddy publishes 80/443. Docker resource limits are sized for this VM:
PostgreSQL 1.5 GB, API 768 MB, worker 768 MB, and Caddy 256 MB.

## Backups within Always Free limits

Keep the active database on the VM but copy encrypted dumps to storage outside
the VM. Before choosing OCI Object Storage, confirm its current Always Free
quota and retention impact. A simple low-cost procedure is:

```bash
sudo install -d -m 700 /var/backups/sentinel
BACKUP_DIR=/var/backups/sentinel ./ops/backup-postgres.sh
sha256sum -c /var/backups/sentinel/sentinel-*.dump.sha256
```

Retain only the required number of compressed/custom-format dumps locally and
copy encrypted archives to a separate approved location. Do not assume the VM
block volume is a backup. Restore only during a maintenance window:

```bash
docker compose --env-file .env.production \
  -f docker-compose.yml -f docker-compose.production.yml stop api worker
CONFIRM_RESTORE=YES ./ops/restore-postgres.sh /var/backups/sentinel/sentinel-<timestamp>.dump
```

Run migrations and the full smoke checklist after restore. A backup is not
valid until it has been restored on an isolated environment.

## Recovery and limitations

- Docker restart policies recover process crashes, but they do not recover a
  reclaimed or unavailable VM.
- Keep a copy of the approved commit, `.env.production` secret source, latest
  backup, and restore instructions outside the instance.
- Monitor disk, memory, certificate expiry, worker liveness, and HTTPS from an
  external location. OCI capacity and account limits can change.
- Do not use the free VM for high-volume monitoring or SMTP throughput without
  measuring CPU, memory, egress, and provider limits.
- Complete [PRODUCTION_SMOKE_TEST.md](PRODUCTION_SMOKE_TEST.md) before calling
  the deployment verified.
