#!/bin/bash
# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================
# Example 4: Backup Monitoring
# Monitor backup status and health, with alerts for failures.

set -e

API_URL="${1:-http://localhost:5000}"
SCHEDULE_ID="${2:-daily-backup}"
SLACK_WEBHOOK="${3:-}"  # Optional Slack webhook for alerts

# Color output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info() {
    echo -e "${GREEN}✓${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}⚠${NC} $1"
}

log_error() {
    echo -e "${RED}✗${NC} $1"
}

send_alert() {
    if [ -z "$SLACK_WEBHOOK" ]; then
        return
    fi

    local message="$1"
    local severity="${2:-error}"

    curl -X POST "$SLACK_WEBHOOK" \
        -H 'Content-Type: application/json' \
        -d "{
            \"text\": \"SQLite Backup Alert\",
            \"attachments\": [{
                \"color\": \"$([ "$severity" = "error" ] && echo "danger" || echo "warning")\",
                \"text\": \"$message\"
            }]
        }" 2>/dev/null || true
}

echo "=== Docker SQLite Backup Monitoring ==="
echo "API: $API_URL"
echo "Schedule: $SCHEDULE_ID"
echo ""

# Health check
echo "Checking health..."
HEALTH=$(curl -s "$API_URL/health")
HEALTH_STATUS=$(echo "$HEALTH" | jq -r '.status // "Unknown"')

if [ "$HEALTH_STATUS" = "Healthy" ]; then
    log_info "System health: $HEALTH_STATUS"
else
    log_error "System health: $HEALTH_STATUS"
fi

echo ""

# Get last backup
echo "Fetching last backup..."
BACKUP_LIST=$(curl -s "$API_URL/api/backup/list?scheduleId=$SCHEDULE_ID&limit=1")
BACKUP_COUNT=$(echo "$BACKUP_LIST" | jq '.data.backups | length')

if [ "$BACKUP_COUNT" -eq 0 ]; then
    log_error "No backups found for schedule: $SCHEDULE_ID"
    send_alert "No backups found for schedule: $SCHEDULE_ID" "error"
    exit 1
fi

LAST_BACKUP=$(echo "$BACKUP_LIST" | jq '.data.backups[0]')
BACKUP_ID=$(echo "$LAST_BACKUP" | jq -r '.id')
BACKUP_TIME=$(echo "$LAST_BACKUP" | jq -r '.createdAt')
BACKUP_STATUS=$(echo "$LAST_BACKUP" | jq -r '.status')
BACKUP_SIZE=$(echo "$LAST_BACKUP" | jq -r '.sizeBytes')
BACKUP_SIZE_MB=$(( BACKUP_SIZE / 1024 / 1024 ))

log_info "Last backup: $BACKUP_ID"
echo "  Time: $BACKUP_TIME"
echo "  Status: $BACKUP_STATUS"
echo "  Size: ${BACKUP_SIZE_MB}MB"

# Check backup status
if [ "$BACKUP_STATUS" != "Success" ]; then
    log_error "Backup failed: $BACKUP_STATUS"
    send_alert "Backup failed for $SCHEDULE_ID: $BACKUP_STATUS" "error"
else
    log_info "Backup completed successfully"
fi

echo ""

# Check verification results
VERIFICATION=$(echo "$LAST_BACKUP" | jq '.verificationResult')
VERIFICATION_VALID=$(echo "$VERIFICATION" | jq -r '.isValid // false')

if [ "$VERIFICATION_VALID" = "true" ]; then
    log_info "Backup verification: PASSED"
    RECORD_COUNT=$(echo "$VERIFICATION" | jq -r '.recordCount')
    echo "  Records: $RECORD_COUNT"
    echo "  Checksum: $(echo "$VERIFICATION" | jq -r '.checksumMatch')"
    echo "  Schema: $(echo "$VERIFICATION" | jq -r '.schemaMatch')"
else
    log_warn "Backup verification: FAILED"
    send_alert "Backup verification failed for $SCHEDULE_ID" "warning"
fi

echo ""

# Backup age check
echo "Checking backup freshness..."
BACKUP_EPOCH=$(date -d "$BACKUP_TIME" +%s 2>/dev/null || date -j -f "%Y-%m-%dT%H:%M:%SZ" "$BACKUP_TIME" +%s)
NOW_EPOCH=$(date +%s)
BACKUP_AGE=$((NOW_EPOCH - BACKUP_EPOCH))
BACKUP_AGE_HOURS=$((BACKUP_AGE / 3600))
BACKUP_AGE_DAYS=$((BACKUP_AGE_HOURS / 24))

if [ $BACKUP_AGE_DAYS -gt 1 ]; then
    log_warn "Backup is $(($BACKUP_AGE_DAYS)) days old"
    send_alert "Backup for $SCHEDULE_ID is $BACKUP_AGE_DAYS days old" "warning"
elif [ $BACKUP_AGE_HOURS -gt 24 ]; then
    log_warn "Backup is $(($BACKUP_AGE_HOURS)) hours old"
else
    log_info "Backup is recent ($(($BACKUP_AGE_HOURS)) hours old)"
fi

echo ""

# List recent backups
echo "Recent backups:"
BACKUPS=$(curl -s "$API_URL/api/backup/list?scheduleId=$SCHEDULE_ID&limit=5")
echo "$BACKUPS" | jq -r '.data.backups[] |
    "  \(.id) - \(.createdAt) - \(.status) - \(.sizeBytes / 1024 / 1024 | floor)MB"'

echo ""
echo "=== Monitoring Summary ==="
echo "Total backups: $(echo "$BACKUPS" | jq '.data.pagination.total')"

# Get statistics
echo ""
echo "Backup Statistics:"
SUCCESS_COUNT=$(curl -s "$API_URL/api/backup/list?scheduleId=$SCHEDULE_ID&limit=100&status=Success" | jq '.data.pagination.total // 0')
FAILED_COUNT=$(curl -s "$API_URL/api/backup/list?scheduleId=$SCHEDULE_ID&limit=100&status=Failed" | jq '.data.pagination.total // 0')
SUCCESS_RATE=$(echo "scale=1; $SUCCESS_COUNT * 100 / ($SUCCESS_COUNT + $FAILED_COUNT)" | bc 2>/dev/null || echo "N/A")

echo "  Successful: $SUCCESS_COUNT"
echo "  Failed: $FAILED_COUNT"
echo "  Success Rate: ${SUCCESS_RATE}%"

# Final status
echo ""
if [ "$BACKUP_STATUS" = "Success" ] && [ "$VERIFICATION_VALID" = "true" ]; then
    log_info "All checks passed"
    exit 0
else
    log_error "Some checks failed"
    exit 1
fi
