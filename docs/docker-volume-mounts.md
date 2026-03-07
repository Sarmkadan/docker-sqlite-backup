# Docker Volume Mount Reference

This reference documents the volume mounts used by the Docker SQLite Backup container, how to configure them correctly, and how to troubleshoot common issues.

## Container Directory Layout

| Path inside container | Purpose |
|---|---|
| `/data` | Source SQLite databases to back up |
| `/backups` | Output directory where backup files are written |
| `/app/appsettings.json` | Optional: override the default application configuration |

These paths are created by the Dockerfile and owned by the non-root `backup` user (UID 1000 / GID 1000).

---

## Minimal docker run Example

```bash
docker run -d \
  --name sqlite-backup \
  -v /path/to/databases:/data \
  -v /path/to/backups:/backups \
  -e AppSettings__DatabasePath=/data/app.db \
  -p 5000:5000 \
  sqlite-backup:latest
```

- `/path/to/databases` — host directory containing the `.db` file(s) to back up.
- `/path/to/backups` — host directory that will receive the generated backup archives.

---

## Docker Compose Configuration

The included `docker-compose.yml` mounts three volumes:

```yaml
volumes:
  - ./data:/data                           # source databases (read-write)
  - ./backups:/backups                     # backup output
  - ./appsettings.json:/app/appsettings.json:ro  # optional config override (read-only)
```

### Named volumes (recommended for production)

Bind mounts (`./data`) work well for development. In production, use named volumes for better portability and easier management:

```yaml
volumes:
  db-data:
  backup-storage:

services:
  sqlite-backup:
    image: sqlite-backup:latest
    volumes:
      - db-data:/data
      - backup-storage:/backups
    environment:
      AppSettings__DatabasePath: /data/app.db
      AppSettings__LocalStoragePath: /backups
```

---

## Backing Up a Database Owned by Another Container

Mount the same named volume that your application container uses for its database:

```yaml
services:
  app:
    image: myapp:latest
    volumes:
      - app-db:/app/data

  sqlite-backup:
    image: sqlite-backup:latest
    volumes:
      - app-db:/data:ro   # mount app's volume as read-only
      - backup-storage:/backups
    environment:
      AppSettings__DatabasePath: /data/app.db
      AppSettings__LocalStoragePath: /backups

volumes:
  app-db:
  backup-storage:
```

The `:ro` flag mounts the source volume as read-only inside the backup container, preventing accidental writes to the live database.

---

## S3 Storage — No Backup Volume Required

When storing backups in S3 you can omit the `/backups` mount. The service writes a temporary local file, uploads it, and then removes the local copy. A minimal S3 setup:

```yaml
volumes:
  - ./data:/data    # still needed for the source database

environment:
  AppSettings__DatabasePath: /data/app.db
  AppSettings__StorageType: S3
  AppSettings__S3BucketName: my-backup-bucket
  AppSettings__S3Region: us-east-1
  AWS_ACCESS_KEY_ID: "<key>"
  AWS_SECRET_ACCESS_KEY: "<secret>"
```

---

## Overriding Application Configuration

Mount a custom `appsettings.json` as read-only to override any default setting without rebuilding the image:

```bash
docker run -d \
  --name sqlite-backup \
  -v /path/to/databases:/data \
  -v /path/to/backups:/backups \
  -v /path/to/custom-appsettings.json:/app/appsettings.json:ro \
  sqlite-backup:latest
```

Individual settings can also be overridden with environment variables using the `__` double-underscore separator:

```bash
-e AppSettings__MaxConcurrentBackups=5
-e AppSettings__BackupTimeoutSeconds=7200
-e Logging__LogLevel__Default=Debug
```

Environment variables take precedence over values in `appsettings.json`.

---

## File Permissions

The container runs as UID 1000 / GID 1000. Host directories mounted at `/data` and `/backups` must be readable (and writable for `/backups`) by that user.

```bash
# Allow the backup user to read sources and write backups
chown -R 1000:1000 /path/to/databases /path/to/backups

# Or use broader permissions if the exact UID does not matter
chmod -R 755 /path/to/databases
chmod -R 775 /path/to/backups
```

If the container cannot read the database file it will log a `DatabaseAccessException` and the backup job will be marked as failed.

---

## Best Practices

- **Use read-only source mounts** (`:ro`) to prevent the backup service from accidentally modifying the live database.
- **Use named volumes in production** rather than bind mounts to avoid host path coupling.
- **Do not mount the SQLite WAL (`.db-wal`) and SHM (`.db-shm`) files individually** — mount the parent directory so all three files are accessible together. The service uses the SQLite Online Backup API and needs the WAL file present to produce a consistent snapshot.
- **Avoid mounting the same `/backups` path as the source database directory** to prevent a backup file from being picked up and re-backed-up on the next run.
- **Set `AppSettings__LocalStoragePath`** to the same path as the `/backups` mount so that backup files and the configured output directory are in sync.

---

## Troubleshooting

### `Database file not found` error
The service cannot find the database at the configured `AppSettings__DatabasePath`.

Checklist:
1. Confirm that the host directory containing the `.db` file is mounted to `/data`.
2. Verify `AppSettings__DatabasePath` matches the in-container path (e.g., `/data/app.db`).
3. Check that the host file exists: `ls -la /path/to/databases/app.db`.

### `Permission denied` when writing backups
The backup user (UID 1000) cannot write to `/backups`.

```bash
# Fix ownership on the host
chown -R 1000:1000 /path/to/backups
```

### Backups appear but are empty or corrupted
This usually means the database file was mounted without its WAL/SHM siblings.

- Mount the entire parent directory, not the individual `.db` file.
- Ensure the source database is not mounted read-only at the OS level while a write is in progress.

### Changes in the mounted `appsettings.json` are not picked up
The application reads configuration at startup. Restart the container after modifying the file:

```bash
docker restart sqlite-backup
```
