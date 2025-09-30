# Architecture Documentation

## System Overview

Docker SQLite Backup is built on a modular, event-driven architecture designed for reliability, scalability, and extensibility. The system consists of several independent layers that communicate through well-defined interfaces.

## Layered Architecture

```
┌─────────────────────────────────────────────────────┐
│           Presentation Layer                        │
│  ┌──────────────┐  ┌──────────────┐                 │
│  │  REST API    │  │  CLI Handler │                 │
│  │  Controllers │  │              │                 │
│  └──────────────┘  └──────────────┘                 │
├─────────────────────────────────────────────────────┤
│           Application Layer                         │
│  ┌──────────────────────────────────────────────┐   │
│  │  Orchestration & Workflow Services           │   │
│  │  - BackupService (Main orchestrator)         │   │
│  │  - ScheduleService (Cron management)         │   │
│  │  - RotationService (Cleanup policy)          │   │
│  │  - VerificationService (Integrity checks)    │   │
│  └──────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────┤
│           Domain Layer                              │
│  ┌──────────────────────────────────────────────┐   │
│  │  Entity Models & Business Logic               │   │
│  │  - BackupSchedule, BackupJob, BackupResult   │   │
│  │  - RotationPolicy, RestoreVerification       │   │
│  │  - Events (BackupEvent, etc.)                │   │
│  └──────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────┤
│           Infrastructure Layer                      │
│  ┌──────────────────────────────────────────────┐   │
│  │  Storage Abstraction                         │   │
│  │  - StorageService (Interface)                │   │
│  │  - LocalStorageProvider                      │   │
│  │  - S3StorageProvider                         │   │
│  └──────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────┤
│           Cross-Cutting Concerns                    │
│  ┌─────────────────────────────────────────────┐    │
│  │  Middleware, Caching, Logging, Events       │    │
│  │  - Exception Handling Middleware             │    │
│  │  - Rate Limiting Middleware                  │    │
│  │  - Audit Logging                            │    │
│  │  - Event Publishing & Listening             │    │
│  │  - Memory Caching                           │    │
│  └─────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────┘
```

## Core Services

### BackupService

The primary orchestrator that coordinates all backup operations.

**Responsibilities:**
- Execute backup operations for scheduled jobs
- Coordinate with storage backends
- Trigger verification service
- Publish backup events
- Manage backup lifecycle

**Key Methods:**
```csharp
Task<BackupResult> ExecuteBackupAsync(BackupJob job, CancellationToken ct);
Task<List<BackupResult>> GetBackupHistoryAsync(string scheduleId, int limit);
Task<bool> TriggerBackupAsync(string scheduleId, BackupTriggerMode mode);
```

### ScheduleService

Manages cron-based backup scheduling and execution timing.

**Responsibilities:**
- Parse and validate cron expressions
- Calculate next run times
- Track schedule state
- Determine which backups should run
- Handle timezone considerations

**Implementation Details:**
- Uses NCronTab library for cron parsing
- Maintains in-memory schedule state
- Runs background check loop every N seconds
- Triggers BackupService when time matches

### RotationService

Implements backup retention and cleanup policies.

**Responsibilities:**
- Apply age-based rotation (delete backups older than N days)
- Apply count-based rotation (keep only last N backups)
- Apply hybrid rotation (age AND count)
- Execute cleanup operations
- Log rotation actions for audit

**Rotation Strategies:**

1. **Age-Based**: Deletes backups older than `RetentionDays`
2. **Count-Based**: Keeps only the most recent `MaxBackupCount` backups
3. **Hybrid**: Applies both constraints (oldest-first deletion)

### VerificationService

Ensures backup integrity and usability.

**Verification Steps:**
1. Download backup file from storage
2. Create temporary database restore
3. Compare schema with original
4. Count records and compare totals
5. Validate checksums
6. Clean up temporary files
7. Record verification result

**Handles:**
- Corrupted backups
- Schema differences
- Missing tables
- Data inconsistencies

### StorageService

Abstract interface supporting multiple storage backends.

**Supported Backends:**
- **Local Filesystem**: Direct file storage
- **AWS S3**: Remote cloud storage with optional encryption
- **S3 with Versioning**: Immutable backup history
- **S3 with Transfer Acceleration**: Fast uploads

**Storage Operations:**
```csharp
Task<string> UploadBackupAsync(string backupPath, string key);
Task<Stream> DownloadBackupAsync(string key);
Task<bool> DeleteBackupAsync(string key);
Task<BackupMetadata[]> ListBackupsAsync(string prefix);
```

## Event-Driven Architecture

The system uses a pub-sub event model for loose coupling and extensibility.

### Event Types

```csharp
public class BackupEvent
{
    public string EventType { get; set; }        // BackupStarted, BackupCompleted, etc.
    public string ScheduleId { get; set; }
    public string JobId { get; set; }
    public BackupStatus Status { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

### Built-in Event Listeners

**MetricsEventListener**
- Records backup duration, size, success rate
- Updates performance metrics
- Integrates with monitoring systems

**NotificationEventListener**
- Sends webhooks on backup events
- Includes backup status and metadata
- Configurable filters (success-only, failures-only, etc.)

**AuditEventListener**
- Logs all backup operations
- Records who, what, when, where
- Maintains compliance audit trail

### Custom Event Listeners

Implement `IBackupEventListener` for custom behavior:

```csharp
public interface IBackupEventListener
{
    Task OnBackupStarted(BackupEvent evt);
    Task OnBackupCompleted(BackupEvent evt);
    Task OnVerificationCompleted(BackupEvent evt);
}

// Register in DI container
services.AddSingleton<IBackupEventListener, MyCustomListener>();
```

## Data Flow

### Backup Execution Flow

```
1. Schedule Triggers
   ↓
2. BackupService.ExecuteBackupAsync()
   ├─ Publish BackupStarted event
   ├─ Create backup snapshot
   ├─ Call StorageService.UploadAsync()
   ├─ Trigger VerificationService (if enabled)
   └─ Publish BackupCompleted event
   ↓
3. RotationService.ApplyRotationAsync()
   ├─ Load retention policy
   ├─ Identify old/excess backups
   ├─ Delete marked backups
   └─ Publish RotationCompleted event
   ↓
4. Event Listeners Process Event
   ├─ MetricsListener: Record metrics
   ├─ NotificationListener: Send webhook
   └─ AuditListener: Log to audit trail
```

### Request Flow (HTTP API)

```
1. HTTP Request arrives at Controller
   ↓
2. RequestContextMiddleware
   ├─ Generate request ID
   ├─ Extract user context
   └─ Add to LogicalCallContext
   ↓
3. RateLimitingMiddleware
   ├─ Check rate limits
   └─ Allow/Reject based on policy
   ↓
4. ExceptionHandlingMiddleware
   ├─ Wrap handler in try-catch
   └─ Convert exceptions to responses
   ↓
5. BackupController Action
   ├─ Validate input
   ├─ Call BackupService
   └─ Return response
   ↓
6. Response sent to client
```

## Concurrency & Thread Safety

### Rate Limiting

Prevents resource exhaustion through concurrent backup operations:

```csharp
// Configuration
{
  "AppSettings": {
    "MaxConcurrentBackups": 3
  }
}

// Implementation: SemaphoreSlim gates concurrent executions
private readonly SemaphoreSlim _backupSemaphore = new(3);

await _backupSemaphore.WaitAsync();
try
{
    // Execute backup
}
finally
{
    _backupSemaphore.Release();
}
```

### Timeout Protection

Prevents hung backups from blocking the system:

```csharp
using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
cts.CancelAfter(TimeSpan.FromSeconds(BackupTimeoutSeconds));

try
{
    await backupTask.ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    // Backup timed out - log and handle
}
```

### Database Access

SQLite write-locks ensure consistent backups via WAL (Write-Ahead Logging).

## Dependency Injection

The application uses Microsoft.Extensions.DependencyInjection:

```csharp
// In Program.cs or Startup.cs
services.AddSingleton<IBackupService, BackupService>();
services.AddSingleton<IScheduleService, ScheduleService>();
services.AddSingleton<IRotationService, RotationService>();
services.AddSingleton<IVerificationService, VerificationService>();
services.AddSingleton<IStorageService, StorageService>();

// Register configuration
services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
```

## Configuration Management

### Hierarchical Configuration Sources

1. **appsettings.json**: Default configuration
2. **appsettings.{Environment}.json**: Environment-specific overrides
3. **Environment Variables**: Runtime overrides using `__` separator
4. **Command-line Arguments**: CLI option overrides

Example override chain:
```
Base config
    ↓ (merged by)
appsettings.Production.json
    ↓ (merged by)
Environment variables
    ↓ (merged by)
CLI arguments
```

### Validation

Configuration is validated at startup:
- Required fields checked
- Cron expressions validated
- Storage paths verified
- S3 credentials validated

## Error Handling Strategy

### Exception Hierarchy

```
Exception
├─ BackupException (Backup operation failures)
├─ StorageException (Storage backend errors)
├─ ScheduleException (Schedule/cron errors)
├─ VerificationException (Verification failures)
└─ ValidationException (Input validation)
```

### Global Exception Handler

```csharp
// Middleware catches all exceptions
try
{
    await _next(context);
}
catch (Exception ex)
{
    await HandleExceptionAsync(context, ex);
}
```

Converts exceptions to appropriate HTTP responses:
- `BackupException` → 400 Bad Request
- `StorageException` → 503 Service Unavailable
- `ValidationException` → 422 Unprocessable Entity
- Others → 500 Internal Server Error

## Scalability Considerations

### Horizontal Scaling

The application can be deployed as multiple instances:

**Load Distribution:**
- Stateless design allows multiple instances
- Each instance runs independent backup schedules
- Use external locking to prevent duplicate backups (optional)

**Recommended Architecture:**
```
Load Balancer
├─ Instance 1 (Backup Service)
├─ Instance 2 (Backup Service)
└─ Instance 3 (Backup Service)
    ↓
Shared Storage (S3 or NFS)
```

### Performance Optimizations

1. **Memory Caching**: Metadata cached to reduce database queries
2. **Async/Await**: Non-blocking I/O for concurrent operations
3. **Streaming**: Large files streamed to avoid memory overflow
4. **Connection Pooling**: Reuse HTTP connections to S3

## Security Architecture

### Data Protection

- **In Transit**: HTTPS for all API calls, S3 SSL encryption
- **At Rest**: Optional S3 encryption, local file permissions
- **Backups**: No sensitive data logged, checksums verified

### Access Control

- Rate limiting prevents DoS
- Request context tracks operations
- Audit logging provides accountability
- API endpoints can be protected with authentication middleware

### Configuration Security

- Use environment variables for secrets (not in appsettings.json)
- AWS credentials from IAM roles (not hardcoded)
- Webhook URLs validated before use
- Database paths validated and isolated

## Testing Strategy

### Unit Tests

- Test individual services in isolation
- Mock dependencies (storage, schedule, etc.)
- Test error conditions and edge cases

### Integration Tests

- Test end-to-end backup flow
- Use real SQLite databases
- Verify storage backend integration
- Test schedule execution

### Performance Tests

- Measure backup duration for large databases
- Test concurrent backup handling
- Monitor memory usage during rotation
