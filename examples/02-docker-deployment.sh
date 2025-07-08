#!/bin/bash
# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================
# Example 2: Docker Deployment
# Deploy SQLite Backup in a Docker container with local and S3 storage.

set -e

CONTAINER_NAME="${1:-sqlite-backup}"
DATABASE_PATH="${2:-/var/lib/app/database.db}"
BACKUPS_PATH="${3:-/var/backups/sqlite}"
AWS_REGION="${4:-us-east-1}"
AWS_BUCKET="${5:-company-backups}"

echo "=== Docker SQLite Backup Deployment ==="
echo "Container: $CONTAINER_NAME"
echo "Database: $DATABASE_PATH"
echo "Backups: $BACKUPS_PATH"
echo ""

# Create directories
mkdir -p "$BACKUPS_PATH"
chmod 755 "$BACKUPS_PATH"

# Build Docker image
echo "Building Docker image..."
docker build -t sqlite-backup:latest . > /dev/null 2>&1
echo "✓ Docker image built"

# Check if container is already running
if docker ps -a --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
    echo "Stopping existing container..."
    docker stop "$CONTAINER_NAME" 2>/dev/null || true
    docker rm "$CONTAINER_NAME" 2>/dev/null || true
fi

# Run container
echo "Starting Docker container..."
docker run -d \
  --name "$CONTAINER_NAME" \
  --restart unless-stopped \
  -v "$DATABASE_PATH:/data/app.db" \
  -v "$BACKUPS_PATH:/app/backups" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e AppSettings__DatabasePath=/data/app.db \
  -e AppSettings__LocalStoragePath=/app/backups \
  -e AppSettings__MaxConcurrentBackups=2 \
  -e AppSettings__BackupTimeoutSeconds=3600 \
  -e AppSettings__EnableVerificationByDefault=true \
  -e AppSettings__RetentionDays=30 \
  -e AppSettings__MaxBackupCount=10 \
  -p 5000:5000 \
  sqlite-backup:latest > /dev/null

echo "✓ Container started: $CONTAINER_NAME"
echo ""

# Wait for container to be ready
echo "Waiting for container to be ready..."
for i in {1..30}; do
    if docker exec "$CONTAINER_NAME" curl -s http://localhost:5000/health > /dev/null 2>&1; then
        echo "✓ Container is ready"
        break
    fi
    sleep 1
done

# Test the health endpoint
echo ""
echo "Health Check:"
docker exec "$CONTAINER_NAME" curl -s http://localhost:5000/health | jq '.'

echo ""
echo "Deployment Summary:"
echo "  Container: $CONTAINER_NAME"
echo "  Status: $(docker inspect -f '{{.State.Running}}' $CONTAINER_NAME)"
echo "  API: http://localhost:5000"
echo ""
echo "Useful commands:"
echo "  View logs: docker logs -f $CONTAINER_NAME"
echo "  Stop: docker stop $CONTAINER_NAME"
echo "  Restart: docker restart $CONTAINER_NAME"
echo "  List backups: docker exec $CONTAINER_NAME ls -lah /app/backups/"
echo "  Trigger backup: curl -X POST http://localhost:5000/api/backup/trigger -H 'Content-Type: application/json' -d '{\"scheduleId\": \"daily-backup\"}'"
