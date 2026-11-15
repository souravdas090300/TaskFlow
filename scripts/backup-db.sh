#!/usr/bin/env bash
# TaskFlow database backup.
# Usage: ./scripts/backup-db.sh [output-dir]
# Requires: docker (uses the running taskflow-db container) and pg_dump inside the postgres image.
set -euo pipefail

CONTAINER="${DB_CONTAINER:-taskflow-db}"
DB_NAME="${DB_NAME:-taskflow}"
DB_USER="${DB_USER:-taskflow}"
OUT_DIR="${1:-./backups}"
STAMP="$(date +%Y%m%d-%H%M%S)"
FILE="$OUT_DIR/taskflow-$STAMP.dump"

mkdir -p "$OUT_DIR"

docker exec "$CONTAINER" pg_dump -U "$DB_USER" -d "$DB_NAME" -Fc > "$FILE"

echo "Backup written to $FILE ($(du -h "$FILE" | cut -f1))"
