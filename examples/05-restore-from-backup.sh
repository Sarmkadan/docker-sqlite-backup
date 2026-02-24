#!/bin/bash
# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================
# Example 5: Restore from Backup
# Demonstrate how to restore a database from a backup.

set -e

API_URL="${1:-http://localhost:5000}"
BACKUP_ID="${2:-}"
OUTPUT_PATH="${3:-.restore-test.db}"
VERIFY="${4:-true}"

echo "=== Docker SQLite Backup - Restore Procedure ==="
echo ""

if [ -z "$BACKUP_ID" ]; then
    echo "Available backups:"
    echo ""

    BACKUPS=$(curl -s "$API_URL/api/backup/list?scheduleId=daily-backup&limit=10")
    echo "$BACKUPS" | jq -r '.data.backups[] |
        .id + " | Created: " + .createdAt + " | Status: " + .status + " | Size: " + (.sizeBytes / 1024 / 1024 | floor | tostring) + "MB"'

    echo ""
    echo "Usage: $0 <api-url> <backup-id> [output-path] [verify]"
    echo "Example: $0 http://localhost:5000 backup-20260504-020000 ./restored.db true"
    exit 1
fi

echo "Restore Configuration:"
echo "  API URL: $API_URL"
echo "  Backup ID: $BACKUP_ID"
echo "  Output Path: $OUTPUT_PATH"
echo "  Verify After Restore: $VERIFY"
echo ""

# Step 1: Get backup details
echo "Step 1: Retrieving backup details..."
BACKUP_DETAILS=$(curl -s "$API_URL/api/backup/$BACKUP_ID")
BACKUP_EXISTS=$(echo "$BACKUP_DETAILS" | jq '.success // false')

if [ "$BACKUP_EXISTS" != "true" ]; then
    echo "ERROR: Backup not found: $BACKUP_ID"
    exit 1
fi

BACKUP_SIZE=$(echo "$BACKUP_DETAILS" | jq '.data.sizeBytes')
BACKUP_STATUS=$(echo "$BACKUP_DETAILS" | jq -r '.data.status')
BACKUP_VERIFIED=$(echo "$BACKUP_DETAILS" | jq '.data.verificationResult.isValid // false')

echo "✓ Backup found"
echo "  Status: $BACKUP_STATUS"
echo "  Size: $(($BACKUP_SIZE / 1024 / 1024))MB"
echo "  Previously Verified: $BACKUP_VERIFIED"
echo ""

# Step 2: Check output path
echo "Step 2: Preparing output location..."
if [ -f "$OUTPUT_PATH" ]; then
    echo "WARNING: File already exists: $OUTPUT_PATH"
    read -p "Overwrite? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Restore cancelled"
        exit 1
    fi
    rm "$OUTPUT_PATH"
fi

OUTPUT_DIR=$(dirname "$OUTPUT_PATH")
if [ ! -d "$OUTPUT_DIR" ]; then
    mkdir -p "$OUTPUT_DIR"
fi

echo "✓ Output location ready: $OUTPUT_PATH"
echo ""

# Step 3: Execute restore
echo "Step 3: Restoring backup..."
RESTORE_RESPONSE=$(curl -s -X POST "$API_URL/api/backup/restore" \
    -H "Content-Type: application/json" \
    -d "{
        \"backupId\": \"$BACKUP_ID\",
        \"outputPath\": \"$OUTPUT_PATH\",
        \"verifyAfterRestore\": $VERIFY
    }")

RESTORE_SUCCESS=$(echo "$RESTORE_RESPONSE" | jq '.success // false')

if [ "$RESTORE_SUCCESS" != "true" ]; then
    echo "ERROR: Restore failed"
    echo "$RESTORE_RESPONSE" | jq '.'
    exit 1
fi

RESTORE_TIME=$(echo "$RESTORE_RESPONSE" | jq -r '.data.restoredAt')
RESTORE_DURATION=$(echo "$RESTORE_RESPONSE" | jq '.data.durationMs')
RESTORE_SIZE=$(echo "$RESTORE_RESPONSE" | jq '.data.fileSize')

echo "✓ Restore completed successfully"
echo "  Restored at: $RESTORE_TIME"
echo "  Duration: $(($RESTORE_DURATION / 1000))s"
echo "  Size: $(($RESTORE_SIZE / 1024 / 1024))MB"
echo ""

# Step 4: Verify restored database
if [ "$VERIFY" = "true" ]; then
    echo "Step 4: Verifying restored database..."

    if ! command -v sqlite3 &> /dev/null; then
        echo "WARNING: sqlite3 not found, skipping verification"
    else
        # Check database integrity
        INTEGRITY=$(sqlite3 "$OUTPUT_PATH" "PRAGMA integrity_check;" 2>&1)
        if [ "$INTEGRITY" = "ok" ]; then
            echo "✓ Database integrity: OK"
        else
            echo "ERROR: Database integrity check failed"
            echo "$INTEGRITY"
            exit 1
        fi

        # Count tables
        TABLE_COUNT=$(sqlite3 "$OUTPUT_PATH" "SELECT COUNT(*) FROM sqlite_master WHERE type='table';" 2>&1)
        echo "✓ Tables found: $TABLE_COUNT"

        # Show table list
        echo ""
        echo "Tables in restored database:"
        sqlite3 "$OUTPUT_PATH" ".tables" 2>&1 | sed 's/^/  /'
    fi
fi

echo ""
echo "=== Restore Summary ==="
echo "Backup: $BACKUP_ID"
echo "Restored to: $OUTPUT_PATH"
echo "Status: SUCCESS"
echo ""
echo "Next steps:"
echo "  1. Verify database is correct: sqlite3 $OUTPUT_PATH '.tables'"
echo "  2. Test queries: sqlite3 $OUTPUT_PATH 'SELECT COUNT(*) FROM table_name;'"
echo "  3. If satisfied, replace production database: cp $OUTPUT_PATH /path/to/production.db"
echo "  4. Clean up: rm $OUTPUT_PATH"
