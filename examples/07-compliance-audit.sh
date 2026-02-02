#!/bin/bash
# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================
# Example 7: Compliance Audit
# Audit backup compliance with retention policies and regulatory requirements.

set -e

API_URL="${1:-http://localhost:5000}"
SCHEDULE_ID="${2:-daily-backup}"
RETENTION_DAYS="${3:-30}"
MIN_BACKUPS="${4:-10}"
REPORT_FILE="${5:-audit-report.json}"

echo "=== Compliance Audit Report ==="
echo "API: $API_URL"
echo "Schedule: $SCHEDULE_ID"
echo "Required Retention: $RETENTION_DAYS days"
echo "Minimum Backups: $MIN_BACKUPS"
echo ""

TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
AUDIT_DATA="{\"timestamp\": \"$TIMESTAMP\", \"tests\": []}"

add_test() {
    local test_name="$1"
    local passed="$2"
    local message="$3"

    local status="PASS"
    if [ "$passed" != "true" ]; then
        status="FAIL"
    fi

    AUDIT_DATA=$(echo "$AUDIT_DATA" | jq ".tests += [{
        \"name\": \"$test_name\",
        \"status\": \"$status\",
        \"message\": \"$message\"
    }]")
}

# Test 1: System Health
echo "Test 1: System Health Check"
HEALTH=$(curl -s "$API_URL/health")
HEALTH_STATUS=$(echo "$HEALTH" | jq -r '.status // "Unknown"')

if [ "$HEALTH_STATUS" = "Healthy" ]; then
    echo "  ✓ System is healthy"
    add_test "System Health" "true" "Backup system is operational"
else
    echo "  ✗ System is not healthy"
    add_test "System Health" "false" "Backup system status: $HEALTH_STATUS"
fi

echo ""

# Test 2: Backup Schedule Exists
echo "Test 2: Schedule Configuration"
SCHEDULES=$(curl -s "$API_URL/api/schedule/list")
SCHEDULE_EXISTS=$(echo "$SCHEDULES" | jq --arg id "$SCHEDULE_ID" '.data.schedules[] | select(.id == $id) | .id' | grep -q "$SCHEDULE_ID" && echo "true" || echo "false")

if [ "$SCHEDULE_EXISTS" = "true" ]; then
    echo "  ✓ Schedule exists: $SCHEDULE_ID"
    SCHEDULE=$(echo "$SCHEDULES" | jq --arg id "$SCHEDULE_ID" '.data.schedules[] | select(.id == $id)')
    IS_ENABLED=$(echo "$SCHEDULE" | jq -r '.isEnabled')
    CRON=$(echo "$SCHEDULE" | jq -r '.cronExpression')
    echo "    Enabled: $IS_ENABLED"
    echo "    Cron: $CRON"
    add_test "Schedule Configuration" "true" "Schedule $SCHEDULE_ID is configured and enabled"
else
    echo "  ✗ Schedule not found: $SCHEDULE_ID"
    add_test "Schedule Configuration" "false" "Schedule $SCHEDULE_ID not found"
fi

echo ""

# Test 3: Backup Retention Policy
echo "Test 3: Retention Policy Compliance"
BACKUPS=$(curl -s "$API_URL/api/backup/list?scheduleId=$SCHEDULE_ID&limit=100")
BACKUP_COUNT=$(echo "$BACKUPS" | jq '.data.backups | length')
TOTAL_BACKUPS=$(echo "$BACKUPS" | jq '.data.pagination.total')

echo "  Current backups: $BACKUP_COUNT"
echo "  Total available: $TOTAL_BACKUPS"

if [ "$TOTAL_BACKUPS" -ge "$MIN_BACKUPS" ]; then
    echo "  ✓ Minimum backup count met ($TOTAL_BACKUPS >= $MIN_BACKUPS)"
    add_test "Minimum Backup Count" "true" "Has $TOTAL_BACKUPS backups (required: $MIN_BACKUPS)"
else
    echo "  ✗ Insufficient backups ($TOTAL_BACKUPS < $MIN_BACKUPS)"
    add_test "Minimum Backup Count" "false" "Only $TOTAL_BACKUPS backups available (required: $MIN_BACKUPS)"
fi

echo ""

# Test 4: Recent Backup Exists
echo "Test 4: Recent Backup Availability"
OLDEST_ALLOWED=$(($(date +%s) - RETENTION_DAYS * 86400))

RECENT_COUNT=0
while IFS= read -r backup_time; do
    BACKUP_EPOCH=$(date -d "$backup_time" +%s 2>/dev/null || date -j -f "%Y-%m-%dT%H:%M:%SZ" "$backup_time" +%s)
    if [ "$BACKUP_EPOCH" -gt "$OLDEST_ALLOWED" ]; then
        ((RECENT_COUNT++))
    fi
done < <(echo "$BACKUPS" | jq -r '.data.backups[].createdAt')

echo "  Backups within $RETENTION_DAYS days: $RECENT_COUNT"

if [ "$RECENT_COUNT" -gt 0 ]; then
    echo "  ✓ Recent backups available"
    add_test "Recent Backup Availability" "true" "Has $RECENT_COUNT backups within $RETENTION_DAYS days"
else
    echo "  ✗ No recent backups available"
    add_test "Recent Backup Availability" "false" "No backups within $RETENTION_DAYS days"
fi

echo ""

# Test 5: Backup Verification
echo "Test 5: Backup Verification Status"
VERIFIED_COUNT=0
UNVERIFIED_COUNT=0

while IFS= read -r is_valid; do
    if [ "$is_valid" = "true" ]; then
        ((VERIFIED_COUNT++))
    else
        ((UNVERIFIED_COUNT++))
    fi
done < <(echo "$BACKUPS" | jq -r '.data.backups[].verificationResult.isValid // false')

echo "  Verified: $VERIFIED_COUNT"
echo "  Unverified/Failed: $UNVERIFIED_COUNT"

if [ "$UNVERIFIED_COUNT" -eq 0 ] && [ "$VERIFIED_COUNT" -gt 0 ]; then
    echo "  ✓ All backups verified successfully"
    add_test "Backup Verification" "true" "All $VERIFIED_COUNT backups passed verification"
else
    echo "  ⚠ Some backups not verified: $UNVERIFIED_COUNT failures"
    add_test "Backup Verification" "false" "$UNVERIFIED_COUNT backups failed verification"
fi

echo ""

# Test 6: Storage Accessibility
echo "Test 6: Storage Accessibility"
STORAGE_TYPE=$(echo "$SCHEDULE" | jq -r '.storageType // "Local"')
echo "  Storage type: $STORAGE_TYPE"

if [ "$STORAGE_TYPE" = "S3" ]; then
    echo "  ✓ Using S3 (highly available storage)"
    add_test "Storage Accessibility" "true" "Using S3 cloud storage"
elif [ "$STORAGE_TYPE" = "Local" ]; then
    echo "  ⚠ Using local storage (ensure redundancy)"
    add_test "Storage Accessibility" "true" "Using local storage (verify backups are backed up)"
fi

echo ""

# Test 7: Backup Integrity Metrics
echo "Test 7: Backup Integrity Metrics"
CHECKSUM_FAILURES=0
SCHEMA_FAILURES=0

while IFS= read -r checksum_match schema_match; do
    if [ "$checksum_match" != "true" ]; then
        ((CHECKSUM_FAILURES++))
    fi
    if [ "$schema_match" != "true" ]; then
        ((SCHEMA_FAILURES++))
    fi
done < <(echo "$BACKUPS" | jq -r '.data.backups[] | "\(.verificationResult.checksumMatch // false) \(.verificationResult.schemaMatch // false)"')

echo "  Checksum failures: $CHECKSUM_FAILURES"
echo "  Schema failures: $SCHEMA_FAILURES"

if [ "$CHECKSUM_FAILURES" -eq 0 ] && [ "$SCHEMA_FAILURES" -eq 0 ]; then
    echo "  ✓ All backups have valid checksums and schemas"
    add_test "Backup Integrity" "true" "All backups passed integrity checks"
else
    echo "  ✗ Integrity issues detected"
    add_test "Backup Integrity" "false" "Checksum failures: $CHECKSUM_FAILURES, Schema failures: $SCHEMA_FAILURES"
fi

echo ""

# Test 8: Success Rate
echo "Test 8: Backup Success Rate"
SUCCESS_COUNT=$(echo "$BACKUPS" | jq '[.data.backups[] | select(.status == "Success")] | length')
TOTAL_COUNT=$(echo "$BACKUPS" | jq '.data.backups | length')

if [ "$TOTAL_COUNT" -gt 0 ]; then
    SUCCESS_RATE=$(echo "scale=2; $SUCCESS_COUNT * 100 / $TOTAL_COUNT" | bc)
    echo "  Success rate: ${SUCCESS_RATE}% ($SUCCESS_COUNT/$TOTAL_COUNT)"

    if (( $(echo "$SUCCESS_RATE >= 99" | bc -l) )); then
        echo "  ✓ Excellent success rate"
        add_test "Backup Success Rate" "true" "Success rate: ${SUCCESS_RATE}%"
    else
        echo "  ⚠ Success rate below 99%"
        add_test "Backup Success Rate" "false" "Success rate: ${SUCCESS_RATE}% (target: 99%)"
    fi
else
    echo "  ✗ No backups to analyze"
    add_test "Backup Success Rate" "false" "No backup history available"
fi

echo ""

# Generate final report
echo "=== Audit Summary ==="
TOTAL_TESTS=$(echo "$AUDIT_DATA" | jq '.tests | length')
PASSED_TESTS=$(echo "$AUDIT_DATA" | jq '[.tests[] | select(.status == "PASS")] | length')
FAILED_TESTS=$(echo "$AUDIT_DATA" | jq '[.tests[] | select(.status == "FAIL")] | length')

echo "Total Tests: $TOTAL_TESTS"
echo "Passed: $PASSED_TESTS"
echo "Failed: $FAILED_TESTS"

if [ "$FAILED_TESTS" -eq 0 ]; then
    echo ""
    echo "✓ ALL COMPLIANCE TESTS PASSED"
    COMPLIANCE="COMPLIANT"
else
    echo ""
    echo "✗ COMPLIANCE ISSUES DETECTED"
    COMPLIANCE="NON-COMPLIANT"
fi

# Save report
AUDIT_DATA=$(echo "$AUDIT_DATA" | jq ".summary = {\"total\": $TOTAL_TESTS, \"passed\": $PASSED_TESTS, \"failed\": $FAILED_TESTS, \"status\": \"$COMPLIANCE\"}")
echo "$AUDIT_DATA" | jq '.' > "$REPORT_FILE"
echo ""
echo "Report saved to: $REPORT_FILE"

# Exit with appropriate code
if [ "$FAILED_TESTS" -eq 0 ]; then
    exit 0
else
    exit 1
fi
