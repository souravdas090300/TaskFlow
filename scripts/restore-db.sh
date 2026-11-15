#!/usr/bin/env bash
# TaskFlow database restore.
# Usage: ./scripts/restore-db.sh <backup-file.dump>
# WARNING: drops and recreates the public schema contents of the target database.
set -euo pipefail

if [ $# -ne 1 ]; then
  echo "Usage: $0 <backup-file.dump>" >&2
  exit 1
fi

FILE="$1"
CONTAINER="${DB_CONTAINER:-taskflow-db}"
DB_NAME="${DB_NAME:-taskflow}"
DB_USER="${DB_USER:-taskflow}"

if [ ! -f "$FILE" ]; then
  echo "Backup file not found: $FILE" >&2
  exit 1
fi

echo "Restoring $FILE into $CONTAINER/$DB_NAME ..."
docker exec -i "$CONTAINER" pg_restore -U "$DB_USER" -d "$DB_NAME" --clean --if-exists < "$FILE"
echo "Restore complete."
