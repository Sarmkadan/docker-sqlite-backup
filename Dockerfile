# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================
# Multi-stage Dockerfile for Docker SQLite Backup
# Optimized for small image size and production security

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS builder

WORKDIR /build

COPY ["docker-sqlite-backup.csproj", "./"]
RUN dotnet restore "docker-sqlite-backup.csproj"

COPY . .
RUN dotnet build -c Release -o /app/build

RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app

RUN apk add --no-cache \
    sqlite \
    ca-certificates \
    && rm -rf /var/cache/apk/*

COPY --from=builder /app/publish .

RUN addgroup -g 1000 backup && \
    adduser -D -u 1000 -G backup backup && \
    mkdir -p /data /backups && \
    chown -R backup:backup /app /data /backups

USER backup

EXPOSE 8080

# The 'healthcheck' subcommand evaluates the persisted health-status.json file
# (last BackupCompletedEvent/BackupFailedEvent/RestoreVerificationCompletedEvent)
# without starting the full host. It exits 0 when healthy, 1 otherwise.
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD ["dotnet", "docker-sqlite-backup.dll", "healthcheck"]

ENTRYPOINT ["dotnet", "docker-sqlite-backup.dll"]

