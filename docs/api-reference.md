# API Reference

Complete REST API documentation for Docker SQLite Backup. All endpoints return JSON responses with standard envelope format.

## Response Envelope

All API responses follow this standard format:

```json
{
  "success": true,
  "data": {},
  "message": "Operation completed successfully",
  "timestamp": "2026-05-04T12:00:00Z",
  "errors": []
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `success` | boolean | Whether the operation succeeded |
| `data` | object | Response data (varies by endpoint) |
| `message` | string | Human-readable message |
| `timestamp` | ISO8601 | Server time when response generated |
| `errors` | array | Array of error details (if any) |

## Base URL

```
http://localhost:5000/api
```

## Authentication

Current version has no authentication. For production, add authentication middleware:

```csharp
services.AddAuthentication("Bearer")
    .AddJwtBearer(options => { ... });

app.UseAuthentication();
app.UseAuthorization();
```

## Error Responses

### 400 Bad Request

```json
{
  "success": false,
  "errors": [
    {
      "field": "scheduleId",
      "message": "Schedule ID is required"
    }
  ]
}
```

### 404 Not Found

```json
{
  "success": false,
  "message": "Backup not found",
  "errors": [
    {
      "code": "BACKUP_NOT_FOUND",
      "message": "Backup with ID 'backup-123' does not exist"
    }
  ]
}
```

### 500 Internal Server Error

```json
{
  "success": false,
  "message": "An error occurred while processing your request",
  "errors": [
    {
      "code": "STORAGE_ERROR",
      "message": "Failed to connect to S3 bucket: Connection timeout"
    }
  ]
}
```

## Backup Endpoints

### POST /backup/trigger

Trigger an immediate backup execution.

**Request:**
```bash
curl -X POST http://localhost:5000/api/backup/trigger \
  -H "Content-Type: application/json" \
  -d '{
    "scheduleId": "daily-backup",
    "storageType": "Local",
    "skipVerification": false
  }'
```

**Request Body:**
```json
{
  "scheduleId": "string",           // Required: Schedule identifier
  "storageType": "Local|S3",       // Optional: Override configured storage
  "skipVerification": false         // Optional: Skip integrity check
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "data": {
    "jobId": "backup-20260504-140000",
    "scheduleId": "daily-backup",
    "status": "Running",
    "startedAt": "2026-05-04T14:00:00Z",
    "estimatedCompletionTime": "2026-05-04T14:05:00Z"
  },
  "message": "Backup triggered successfully"
}
```

**Status Codes:**
- `201 Created`: Backup triggered
- `400 Bad Request`: Invalid input
- `404 Not Found`: Schedule not found
- `429 Too Many Requests`: Rate limit exceeded
- `503 Service Unavailable`: Storage backend unavailable

---

### GET /backup/list

List backups for a schedule with pagination and filtering.

**Request:**
```bash
curl "http://localhost:5000/api/backup/list?scheduleId=daily-backup&limit=10&offset=0&status=Success"
```

**Query Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `scheduleId` | string | Yes | Schedule identifier |
| `limit` | integer | No | Results per page (default: 10, max: 100) |
| `offset` | integer | No | Pagination offset (default: 0) |
| `status` | string | No | Filter by status: Pending, Running, Success, Failed |
| `startDate` | ISO8601 | No | Filter from date |
| `endDate` | ISO8601 | No | Filter to date |

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "backups": [
      {
        "id": "backup-20260504-020000",
        "scheduleId": "daily-backup",
        "createdAt": "2026-05-04T02:00:00Z",
        "completedAt": "2026-05-04T02:05:30Z",
        "sizeBytes": 1048576,
        "status": "Success",
        "storageType": "Local",
        "storagePath": "backups/backup-20260504-020000.db",
        "checksum": "sha256:abc123...",
        "verificationResult": {
          "isValid": true,
          "verifiedAt": "2026-05-04T02:10:00Z",
          "recordCount": 15234,
          "checksumMatch": true,
          "schemaMatch": true,
          "message": "Backup verified successfully"
        }
      }
    ],
    "pagination": {
      "total": 30,
      "limit": 10,
      "offset": 0,
      "hasMore": true
    }
  }
}
```

---

### GET /backup/{backupId}

Get details of a specific backup.

**Request:**
```bash
curl http://localhost:5000/api/backup/backup-20260504-020000
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "id": "backup-20260504-020000",
    "scheduleId": "daily-backup",
    "createdAt": "2026-05-04T02:00:00Z",
    "completedAt": "2026-05-04T02:05:30Z",
    "durationMs": 330000,
    "sizeBytes": 1048576,
    "status": "Success",
    "storageType": "Local",
    "storagePath": "backups/backup-20260504-020000.db",
    "checksum": "sha256:abc123def456...",
    "verificationResult": {
      "isValid": true,
      "verifiedAt": "2026-05-04T02:10:00Z",
      "recordCount": 15234,
      "checksumMatch": true,
      "schemaMatch": true
    }
  }
}
```

---

### POST /backup/verify/{scheduleId}

Manually verify a backup's integrity.

**Request:**
```bash
curl -X POST http://localhost:5000/api/backup/verify/daily-backup \
  -H "Content-Type: application/json" \
  -d '{
    "backupId": "backup-20260504-020000"
  }'
```

**Request Body:**
```json
{
  "backupId": "string"  // Required: Backup identifier
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "backupId": "backup-20260504-020000",
    "isValid": true,
    "verifiedAt": "2026-05-04T14:30:00Z",
    "durationMs": 45000,
    "recordCount": 15234,
    "checksumMatch": true,
    "schemaMatch": true,
    "message": "Backup verified successfully"
  },
  "message": "Verification completed"
}
```

**Status Codes:**
- `200 OK`: Verification completed
- `400 Bad Request`: Invalid backup ID
- `404 Not Found`: Backup not found
- `503 Service Unavailable`: Storage backend unavailable

---

### DELETE /backup/{backupId}

Delete a backup permanently.

**Request:**
```bash
curl -X DELETE http://localhost:5000/api/backup/backup-20260504-020000
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "backupId": "backup-20260504-020000",
    "deletedAt": "2026-05-04T14:35:00Z",
    "recoveryAvailable": false
  },
  "message": "Backup deleted successfully"
}
```

---

### POST /backup/restore

Restore a backup to a specified location.

**Request:**
```bash
curl -X POST http://localhost:5000/api/backup/restore \
  -H "Content-Type: application/json" \
  -d '{
    "backupId": "backup-20260504-020000",
    "outputPath": "/tmp/restored.db",
    "verifyAfterRestore": true
  }'
```

**Request Body:**
```json
{
  "backupId": "string",            // Required: Backup to restore
  "outputPath": "string",          // Required: Where to restore
  "overwrite": false,              // Optional: Overwrite if exists
  "verifyAfterRestore": true       // Optional: Verify after restore
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "backupId": "backup-20260504-020000",
    "outputPath": "/tmp/restored.db",
    "restoredAt": "2026-05-04T14:40:00Z",
    "durationMs": 60000,
    "fileSize": 1048576,
    "verificationResult": {
      "isValid": true,
      "recordCount": 15234
    }
  },
  "message": "Backup restored successfully"
}
```

## Schedule Endpoints

### GET /schedule/list

List all configured backup schedules.

**Request:**
```bash
curl http://localhost:5000/api/schedule/list
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "schedules": [
      {
        "id": "daily-backup",
        "name": "Daily Backup",
        "description": "Daily backup at 2 AM",
        "cronExpression": "0 2 * * *",
        "isEnabled": true,
        "createdAt": "2026-01-01T00:00:00Z",
        "lastRun": "2026-05-04T02:00:00Z",
        "nextRun": "2026-05-05T02:00:00Z",
        "lastRunStatus": "Success",
        "storageType": "Local",
        "rotationStrategy": "AgeAndCount",
        "retentionDays": 30,
        "maxBackupCount": 10
      }
    ]
  }
}
```

---

### GET /schedule/{scheduleId}

Get details of a specific schedule.

**Request:**
```bash
curl http://localhost:5000/api/schedule/daily-backup
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "id": "daily-backup",
    "name": "Daily Backup",
    "cronExpression": "0 2 * * *",
    "isEnabled": true,
    "lastRun": "2026-05-04T02:00:00Z",
    "nextRun": "2026-05-05T02:00:00Z",
    "lastRunStatus": "Success",
    "consecutiveFailures": 0,
    "totalRuns": 124,
    "successRate": 0.98,
    "averageDurationMs": 330000,
    "averageSizeBytes": 1048576
  }
}
```

---

### POST /schedule/{scheduleId}/enable

Enable a schedule.

**Request:**
```bash
curl -X POST http://localhost:5000/api/schedule/daily-backup/enable
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "scheduleId": "daily-backup",
    "isEnabled": true,
    "enabledAt": "2026-05-04T14:45:00Z"
  }
}
```

---

### POST /schedule/{scheduleId}/disable

Disable a schedule.

**Request:**
```bash
curl -X POST http://localhost:5000/api/schedule/daily-backup/disable
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "scheduleId": "daily-backup",
    "isEnabled": false,
    "disabledAt": "2026-05-04T14:46:00Z"
  }
}
```

## Health & Status Endpoints

### GET /health

Health check for orchestration systems (Kubernetes, Docker).

**Request:**
```bash
curl http://localhost:5000/health
```

**Response (200 OK):**
```json
{
  "status": "Healthy",
  "timestamp": "2026-05-04T14:50:00Z",
  "version": "1.2.0",
  "uptime": "7.12:30:45",
  "checks": {
    "storage": "Healthy",
    "database": "Healthy",
    "scheduler": "Healthy",
    "lastBackup": {
      "scheduleId": "daily-backup",
      "timestamp": "2026-05-04T02:00:00Z",
      "status": "Success"
    }
  }
}
```

---

### GET /health/detailed

Detailed health information for monitoring.

**Request:**
```bash
curl http://localhost:5000/health/detailed
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "overallStatus": "Healthy",
    "componentStatus": {
      "storageBackend": {
        "status": "Healthy",
        "details": "S3 bucket accessible",
        "lastChecked": "2026-05-04T14:50:00Z"
      },
      "database": {
        "status": "Healthy",
        "details": "Database connection successful",
        "size": "1.2 GB"
      },
      "scheduler": {
        "status": "Healthy",
        "activeSchedules": 3,
        "nextScheduledRun": "2026-05-05T02:00:00Z"
      },
      "verification": {
        "status": "Healthy",
        "lastVerification": "2026-05-04T02:10:00Z",
        "pendingVerifications": 0
      }
    },
    "metrics": {
      "totalBackups": 124,
      "successfulBackups": 122,
      "failedBackups": 2,
      "averageDuration": "5m 30s",
      "averageSize": "1 MB",
      "totalStorageUsed": "128 MB"
    }
  }
}
```

---

### GET /metrics

Prometheus-compatible metrics endpoint.

**Request:**
```bash
curl http://localhost:5000/metrics
```

**Response (200 OK):**
```
# HELP backup_total_count Total number of backups executed
# TYPE backup_total_count counter
backup_total_count{schedule="daily-backup",status="success"} 122
backup_total_count{schedule="daily-backup",status="failed"} 2

# HELP backup_duration_seconds Duration of backup operations
# TYPE backup_duration_seconds histogram
backup_duration_seconds{schedule="daily-backup",le="60"} 5
backup_duration_seconds{schedule="daily-backup",le="300"} 120
backup_duration_seconds{schedule="daily-backup",le="+Inf"} 122

# HELP backup_size_bytes Size of backups
# TYPE backup_size_bytes gauge
backup_size_bytes{schedule="daily-backup"} 1048576
```

---

## Rate Limiting

The API implements rate limiting to prevent abuse:

**Default Limits:**
- 100 requests per minute per IP
- 10 backup triggers per minute per schedule
- 50 verification requests per hour

**Response Headers:**
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1262304000
```

**Rate Limited Response (429):**
```json
{
  "success": false,
  "message": "Too many requests",
  "retryAfter": 30
}
```

---

## Pagination

List endpoints support cursor-based and offset-based pagination:

**Offset Pagination:**
```
GET /api/backup/list?limit=20&offset=40
```

**Response Includes:**
```json
{
  "pagination": {
    "total": 124,
    "limit": 20,
    "offset": 40,
    "hasMore": true
  }
}
```

---

## Filtering

Endpoints support filtering with query parameters:

**Examples:**
```
GET /api/backup/list?status=Success&startDate=2026-05-01&endDate=2026-05-04
GET /api/schedule/list?enabled=true
```

Supported filters vary by endpoint. See individual endpoint documentation.

---

## Webhook Events

When webhooks are configured, the following events are posted:

```json
{
  "eventType": "BackupCompleted",
  "scheduleId": "daily-backup",
  "jobId": "backup-20260504-020000",
  "status": "Success",
  "timestamp": "2026-05-04T02:05:30Z",
  "data": {
    "sizeBytes": 1048576,
    "durationMs": 330000,
    "storagePath": "backups/backup-20260504-020000.db"
  }
}
```

Webhook retry policy:
- Initial: Immediate
- Retry 1: 30 seconds
- Retry 2: 5 minutes
- Retry 3: 1 hour
- Max: 3 retries
