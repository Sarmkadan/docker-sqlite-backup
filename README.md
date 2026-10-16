# Docker SQLite Backup

.NET 10 worker service that automates SQLite backups inside Docker: cron scheduling, compression, AES encryption, checksum/integrity verification, rotation, and upload to local/S3/Azure storage.

## Quick Start

The easiest way to get started is using `docker-compose`:

```yaml
version: '3.8'
services:
  sqlite-backup:
    build:
      context: .
      dockerfile: Dockerfile
    image: sqlite-backup:latest
    container_name: sqlite-backup
    restart: unless-stopped
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: "http://0.0.0.0:8080"
      AppSettings__DatabasePath: /data/app.db
      AppSettings__LocalStoragePath: /backups
      AppSettings__MaxConcurrentBackups: "3"
      AppSettings__BackupTimeoutSeconds: "3600"
      AppSettings__EnableVerificationByDefault: "true"
      AppSettings__RetentionDays: "30"
      AppSettings__MaxBackupCount: "10"
      Logging__LogLevel__Default: "Information"
    volumes:
      - ./data:/data
      - ./backups:/backups
      - ./appsettings.json:/app/appsettings.json:ro
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 10s
    networks:
      - backup-network

networks:
  backup-network:
    driver: bridge
```

### Required Environment Variables

The following environment variables are used (with defaults as shown in the docker-compose.yml):

- `AppSettings__DatabasePath`: Path to the SQLite database file (default: `/data/app.db`)
- `AppSettings__LocalStoragePath`: Path to store backups (default: `/backups`)
- `AppSettings__MaxConcurrentBackups`: Maximum number of concurrent backups (default: `3`)
- `AppSettings__BackupTimeoutSeconds`: Timeout for each backup in seconds (default: `3600`)
- `AppSettings__EnableVerificationByDefault`: Whether to enable verification by default (default: `true`)
- `AppSettings__RetentionDays`: Number of days to retain backups (default: `30`)
- `AppSettings__MaxBackupCount`: Maximum number of backups to retain (default: `10`)
- `Logging__LogLevel__Default`: Default log level (default: `Information`)

### Verifying the First Backup

After starting the stack with `docker compose up -d`, you can verify the first backup by:

1. Checking the logs: `docker compose logs -f sqlite-backup`
2. Looking for a log entry indicating a backup has been started and completed.
3. Checking the backups directory: `ls -la ./backups` (on the host) should show a backup file.

Alternatively, you can check the health endpoint: `curl http://localhost:8080/health` should return a 200 OK when the service is healthy.

## Restore a backup

Backups are plain files in the local storage directory (`AppSettings__LocalStoragePath`, `/backups` in the container, `./backups` on the host). File names follow `backup_<schedule>_<yyyy-MM-dd_HH-mm-ss>.sqlite`, with a `.gz` suffix when compression is enabled. Encrypted backups have an additional `.enc` suffix.

There is no restore command in the CLI. Restoring is a manual file copy with the application stopped.

1. **List the backups** (newest first):

   ```bash
   ls -lt ./backups
   # or from inside the container
   docker compose exec sqlite-backup ls -lt /backups
   ```

2. **Prepare the file.** Decompress `.gz` backups and check the result:

   ```bash
   gunzip -k ./backups/backup_<schedule>_<timestamp>.sqlite.gz
   sqlite3 ./backups/backup_<schedule>_<timestamp>.sqlite "PRAGMA integrity_check;"   # expect: ok
   ```

   `.enc` backups must be decrypted with the application's encryption key (`BACKUP_ENCRYPTION_KEY`) before they can be used. See `docs/encryption.md`.

3. **Stop the application** that uses the database, so it is not writing while you copy:

   ```bash
   docker compose stop <app-service>
   ```

4. **Keep the current database and copy the backup into the data volume.** The example below uses the `./data:/data` bind mount from the Quick Start, where the database path is `/data/app.db`:

   ```bash
   cp ./data/app.db ./data/app.db.pre-restore 2>/dev/null || true
   cp ./backups/backup_<schedule>_<timestamp>.sqlite ./data/app.db
   rm -f ./data/app.db-wal ./data/app.db-shm   # stale WAL files from the old database, if present
   ```

   If the data is in a named volume rather than a bind mount, use `docker cp ./backups/<file> <app-container>:/data/app.db` while the container is stopped.

5. **Restart the application:**

   ```bash
   docker compose start <app-service>
   # or: docker compose up -d <app-service>
   ```

## Retention configuration

**Current behaviour:**

- Deletion happens during rotation. Rotation uses one `RotationPolicy` row per backup schedule, stored in the `RotationPolicies` table. If a schedule has no row, rotation does nothing. No code in this repository creates a row outside of tests, so by default no backups are deleted.
- `AppSettings__RetentionDays` and `AppSettings__MaxBackupCount` are bound from configuration, but no service reads them. Setting them does not change what gets deleted.
- `RetentionPolicy` (`src/Domain/RetentionPolicy.cs`) models daily, weekly and monthly tiers through `DailyRetentionDays`, `WeeklyRetentionWeeks`, `MonthlyRetentionMonths` and `MaxTotalStorageBytes`. It is bound from the `RetentionPolicy` configuration section and validated at startup (keep-counts and storage cap must not be negative; at least one tier must be greater than zero). Nothing enforces it at runtime, so setting these values does not change what gets deleted.

**`RotationPolicy` fields** (`src/Domain/RotationPolicy.cs`, saved with `IRotationService.SaveRotationPolicyAsync`):

| Field | Default | Meaning |
|---|---|---|
| `Strategy` | `2` (`Combined`) | `RotationStrategy` value: `0` MaxFileCount, `1` MaxAge, `2` Combined, `3` NoRotation |
| `MaxBackupCount` | `10` | Count limit for the count rule. `0` means unlimited |
| `MaxAgeDays` | `30` | Age limit in days for the age rule |
| `MinimumBackupCount` | `3` | The newest N backups are never deleted |
| `VerifyBeforeDeletion` | `false` | Only delete backups whose status is verified |
| `DeleteFailedBackups` | `true` | Delete failed backups during rotation |

**Example configurations** (C#, applied once per schedule; `schedule.Id` is the schedule's GUID):

Keep the newest 10 backups:

```csharp
await rotationService.SaveRotationPolicyAsync(new RotationPolicy
{
    ScheduleId = schedule.Id,
    Strategy = (int)RotationStrategy.MaxFileCount,
    MaxBackupCount = 10,
    MinimumBackupCount = 3
});
```

Keep 30 days of backups, always keeping at least 3:

```csharp
await rotationService.SaveRotationPolicyAsync(new RotationPolicy
{
    ScheduleId = schedule.Id,
    Strategy = (int)RotationStrategy.MaxAge,
    MaxAgeDays = 30,
    MinimumBackupCount = 3
});
```

Keep up to 14 backups and delete anything older than 30 days, deleting only verified backups:

```csharp
await rotationService.SaveRotationPolicyAsync(new RotationPolicy
{
    ScheduleId = schedule.Id,
    Strategy = (int)RotationStrategy.Combined,
    MaxBackupCount = 14,
    MaxAgeDays = 30,
    MinimumBackupCount = 7,
    VerifyBeforeDeletion = true
});
```

`RotationStrategy` is in `DockerSqliteBackup.Constants`, and `RotationPolicy` is in `DockerSqliteBackup.Domain`. A 7-daily / 4-weekly layout is not expressible with `RotationPolicy` alone. It is the shape `RetentionPolicy` describes, but that class is not wired up yet.