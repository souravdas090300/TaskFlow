# Backup & Rollback

## Database backups

`scripts/backup-db.sh` runs `pg_dump` inside the running `taskflow-db` container
and writes a compressed custom-format dump to `./backups/` (override with a path
argument or `DB_CONTAINER` / `DB_NAME` / `DB_USER` env vars).

```bash
./scripts/backup-db.sh                  # -> ./backups/taskflow-YYYYMMDD-HHMMSS.dump
./scripts/backup-db.sh /mnt/backups     # custom output dir
```

Schedule it with cron on the VPS, e.g. daily at 03:00:

```cron
0 3 * * * cd /opt/taskflow && ./scripts/backup-db.sh /mnt/backups/taskflow
```

Off-site copies: sync the backup directory to object storage (e.g. `rclone` to
S3/Backblaze) — the dump contains customer data, treat it accordingly.

## Database restore

```bash
./scripts/restore-db.sh backups/taskflow-20261115-030000.dump
```

`pg_restore --clean --if-exists` drops existing objects before recreating them.
Take a fresh backup first if there is any data you might need to keep.

## Application rollback

The API image is immutable per deploy (`${DOCKER_IMAGE_API}` in
`docker-compose.prod.yml`), so rollback = redeploy the previous image tag:

```bash
# On the VPS
git checkout <previous-good-commit>          # or just update .env image tags
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d
```

If the bad deploy included an EF migration, restore the database dump **before**
restarting the old image — old code may not understand the new schema. Order:

1. `docker compose stop api frontend`
2. `./scripts/restore-db.sh <pre-deploy dump>`
3. Check out the previous release / image tag
4. `docker compose up -d`

## Monitoring

- `/health` — liveness, no dependencies
- `/db-health` — verifies the DB accepts connections
- Point your uptime monitor (UptimeRobot, Better Stack, ...) at `https://<domain>/health`
- Container logs: `docker compose -f docker-compose.prod.yml logs -f api`
