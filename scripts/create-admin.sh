#!/usr/bin/env bash
# Create or promote a TaskFlow admin user.
#
# Usage:
#   bash scripts/create-admin.sh <email> <password> [firstName] [lastName]
#
# Optional env overrides:
#   BASE_URL       default: https://taskflow-das.duckdns.org
#   DB_CONTAINER   default: taskflow-db        (use taskflow-db-dev for dev compose)
#   DB_USER        default: taskflow
#   DB_NAME        default: taskflow
#
# The password is passed as an argument and is never written to the repo.

set -euo pipefail

EMAIL="${1:?Usage: create-admin.sh <email> <password> [firstName] [lastName]}"
PASSWORD="${2:?password is required}"
FIRST_NAME="${3:-Admin}"
LAST_NAME="${4:-User}"
BASE_URL="${BASE_URL:-https://taskflow-das.duckdns.org}"
DB_CONTAINER="${DB_CONTAINER:-taskflow-db}"
DB_USER="${DB_USER:-taskflow}"
DB_NAME="${DB_NAME:-taskflow}"

if command -v jq >/dev/null 2>&1; then
  payload=$(jq -n \
    --arg email "$EMAIL" \
    --arg password "$PASSWORD" \
    --arg firstName "$FIRST_NAME" \
    --arg lastName "$LAST_NAME" \
    '{email: $email, password: $password, firstName: $firstName, lastName: $lastName}')
else
  payload=$(printf '{"email":"%s","password":"%s","firstName":"%s","lastName":"%s"}' \
    "$EMAIL" "$PASSWORD" "$FIRST_NAME" "$LAST_NAME")
fi

response_file="$(mktemp)"
trap 'rm -f "$response_file"' EXIT

status="$(curl -sS -o "$response_file" -w '%{http_code}' -X POST \
  "$BASE_URL/api/auth/register" \
  -H 'Content-Type: application/json' \
  --data-raw "$payload" || true)"

if [ "$status" != "200" ] && [ "$status" != "201" ]; then
  if grep -q "Email already exists" "$response_file"; then
    echo "Account already exists; promoting to admin."
  else
    echo "Register request failed with HTTP $status" >&2
    cat "$response_file" >&2
    exit 1
  fi
fi

docker exec -i "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" \
  -v ON_ERROR_STOP=1 \
  -v email="$EMAIL" <<'SQL'
UPDATE "Users" SET "Role"='Admin' WHERE "Email" = :'email';
SQL

echo "Admin ready: $EMAIL"
echo "Sign in at: $BASE_URL/admin/login"
