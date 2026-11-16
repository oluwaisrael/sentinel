#!/usr/bin/env bash
set -euo pipefail

archive="${1:-}"
if [[ -z "$archive" || ! -f "$archive" ]]; then
    echo "Usage: CONFIRM_RESTORE=YES $0 /path/to/sentinel-*.dump" >&2
    exit 2
fi
if [[ "${CONFIRM_RESTORE:-}" != YES ]]; then
    echo "Refusing restore. Set CONFIRM_RESTORE=YES after stopping API and worker." >&2
    exit 2
fi

compose_args=(docker compose --env-file "${ENV_FILE:-.env.production}" -f docker-compose.yml -f docker-compose.production.yml)
"${compose_args[@]}" exec -T postgres sh -c 'pg_restore --clean --if-exists --no-owner -U "$POSTGRES_USER" -d "$POSTGRES_DB"' < "$archive"
echo "Restore completed. Run the migration step and smoke tests before restarting traffic."
