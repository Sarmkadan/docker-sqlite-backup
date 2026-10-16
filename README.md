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