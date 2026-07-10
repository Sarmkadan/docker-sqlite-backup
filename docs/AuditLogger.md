# AuditLogger

`AuditLogger` is a structured logging utility used in `docker-sqlite-backup` to record audit events related to backup operations, configuration changes, and data access. It captures contextual metadata such as timestamps, user identities, operation outcomes, and correlation identifiers to support traceability and compliance.

## API

### `public AuditLogger()`
Initializes a new instance of the `AuditLogger` with default values for all properties. The `Timestamp` is set to the current UTC time, and `Success` defaults to `true`. Other properties (`Category`, `Action`, `TargetId`, `UserId`, `Details`, `CorrelationId`) must be set explicitly before logging.

### `public void LogBackupOperation(string targetId, bool success, string? details = null)`
Records an audit event for a backup operation.

- **targetId**: Identifier of the target database or resource being backed up.
- **success**: Indicates whether the backup operation completed successfully.
- **details**: Optional additional context about the operation outcome or errors.
- **Throws**: `ArgumentNullException` if `targetId` is `null`.

### `public void LogScheduleChange(string targetId, bool success, string? details = null)`
Records an audit event for a change to the backup schedule configuration.

- **targetId**: Identifier of the schedule or resource affected by the change.
- **success**: Indicates whether the schedule change was applied successfully.
- **details**: Optional additional context about the change or errors encountered.
- **Throws**: `ArgumentNullException` if `targetId` is `null`.

### `public void LogConfigChange(string targetId, bool success, string? details = null)`
Records an audit event for a configuration change affecting the backup system.

- **targetId**: Identifier of the configuration entity or resource modified.
- **success**: Indicates whether the configuration change was applied successfully.
- **details**: Optional additional context about the change or errors encountered.
- **Throws**: `ArgumentNullException` if `targetId` is `null`.

### `public void LogDataAccess(string targetId, bool success, string? details = null)`
Records an audit event for access to backup or database data.

- **targetId**: Identifier of the data resource accessed.
- **success**: Indicates whether the data access completed successfully.
- **details**: Optional additional context about the access or errors encountered.
- **Throws**: `ArgumentNullException` if `targetId` is `null`.

### `public void LogEntry()`
Records the current state of the audit logger as an audit event. This method uses the current values of all properties (`Timestamp`, `Category`, `Action`, `TargetId`, `UserId`, `Success`, `Details`, `CorrelationId`) to construct and emit the log entry. No parameters are accepted; all data must be set prior to calling this method.

### `public DateTime Timestamp`
Gets or sets the UTC timestamp of the audit event. Defaults to the time of instantiation if not overridden.

### `public string Category`
Gets or sets the category of the audit event (e.g., "Backup", "Schedule", "Config", "Access"). Must be set before calling `LogEntry()`.

### `public string Action`
Gets or sets the specific action being audited (e.g., "BackupInitiated", "ScheduleUpdated", "ConfigModified", "DataRead"). Must be set before calling `LogEntry()`.

### `public string TargetId`
Gets or sets the identifier of the target resource involved in the audited action. Must be set before calling `LogEntry()` or any of the specialized logging methods.

### `public string? UserId`
Gets or sets the identifier of the user or system responsible for the action. Optional; may be `null`.

### `public bool Success`
Gets or sets a value indicating whether the audited action completed successfully. Defaults to `true`.

### `public string? Details`
Gets or sets additional contextual information about the event, such as error messages or state changes. Optional; may be `null`.

### `public Guid CorrelationId`
Gets or sets a unique identifier for correlating related audit events across systems or logs. Defaults to a new `Guid` if not set.

### `public override string ToString()`
Returns a human-readable string representation of the audit event, combining all relevant properties in a structured format. Includes `Timestamp`, `Category`, `Action`, `TargetId`, `UserId`, `Success`, `Details`, and `CorrelationId`. No parameters or return value beyond the formatted string.

## Usage
