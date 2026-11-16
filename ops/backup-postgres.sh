#!/usr/bin/env bash
set -euo pipefail

backup_dir="${BACKUP_DIR:-/var/backups/sentinel}"
compose_args=(docker compose --env-file "${ENV_FILE:-.env.production}" -f docker-compose.yml -f docker-compose.production.yml)
mkdir -p "$backup_dir"
chmod 700 "$backup_dir"

stamp=$(date -u +%Y%m%dT%H%M%SZ)
archive="$backup_dir/sentinel-$stamp.dump"
"${compose_args[@]}" exec -T postgres sh -c 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc' > "$archive"
chmod 600 "$archive"
sha256sum "$archive" > "$archive.sha256"

find "$backup_dir" -type f -name 'sentinel-*.dump' -mtime +"${BACKUP_RETENTION_DAYS:-30}" -delete
find "$backup_dir" -type f -name 'sentinel-*.dump.sha256' -mtime +"${BACKUP_RETENTION_DAYS:-30}" -delete
echo "Created $archive"
