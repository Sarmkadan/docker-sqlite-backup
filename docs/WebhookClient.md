# WebhookClient

Sends JSON webhook notifications about backup events (backup completed, schedule created/updated/etc.) to an HTTP endpoint. Failed requests are retried with exponential backoff only for transient responses (`503 Service Unavailable` and `408 Request Timeout`). When a shared secret is configured, every request carries an `X-Signature` header containing an HMAC-SHA256 signature of the JSON body so the receiver can verify authenticity and freshness.

It is used by `NotificationEventListener` to forward backup events to external systems. It is a plain class (not an interface-backed service), so it can be constructed directly or registered in DI by the host.

Namespace: `DockerSqliteBackup.Integration`

## Behavior Summary

- Uses a shared static `HttpClient` with a 30-second timeout and `User-Agent: docker-sqlite-backup/2.0`.
- Serializes payloads with `System.Text.Json` and posts them as `application/json` (UTF-8).
- Retries only on `503` and `408`, up to `maxRetries` times, with delays of `100ms * 2^attempt` (100 ms, 200 ms, 400 ms, ...).
- Any other non-success status is logged as an error and not retried.
- Transport exceptions are logged and swallowed. The only exception that propagates is `OperationCanceledException` when the caller's `CancellationToken` is cancelled.
- Signing happens only when a non-empty secret is available (from the constructor or `AppSettings.WebhookSecret`).

## API

### WebhookClient(ILogger<WebhookClient> logger, int maxRetries = 3, string? webhookSecret = null)

**Purpose**
Creates a client with an explicit retry limit and optional signing secret.

**Parameters**
- `logger` – Logger used for informational, warning, and error messages.
- `maxRetries` – Maximum number of retry attempts after the first request for `503`/`408` responses. Defaults to `3`.
- `webhookSecret` – Shared secret used to sign payloads. If `null` or empty, no `X-Signature` header is sent.

**Return**
A new `WebhookClient` instance.

**Throws**
None.

### WebhookClient(ILogger<WebhookClient> logger, AppSettings settings, int maxRetries = 3)

**Purpose**
Creates a client that takes the signing secret from `AppSettings.WebhookSecret`.

**Parameters**
- `logger` – Logger instance.
- `settings` – Application settings. `settings.WebhookSecret` is used as the signing secret.
- `maxRetries` – Maximum number of retry attempts. Defaults to `3`.

**Return**
A new `WebhookClient` instance.

**Throws**
`NullReferenceException` if `settings` is `null`.

### string CreateSignatureHeader(string payload, string? secret = null)

**Purpose**
Computes the `X-Signature` header value for a JSON payload. The timestamp is the current Unix time in seconds (UTC).

**Parameters**
- `payload` – The exact JSON string that will be sent as the request body. The signature is only valid for this exact byte sequence.
- `secret` – Optional secret that overrides the constructor secret for this call. If `null`, the constructor secret is used.

**Return**
A string in the format `t=<unix-seconds>,v1=<lowercase-hex-hmac-sha256>`, or `string.Empty` if no secret is available (a warning is logged in that case).

**Throws**
None.

**Notes**
The HMAC key is the UTF-8 bytes of the secret; the message is the UTF-8 bytes of `payload`. The hex digest is lowercase with no separators.

### Task SendBackupNotificationAsync(string webhookUrl, BackupResult result, CancellationToken cancellationToken = default)

**Purpose**
Sends a `backup.completed` event describing a finished backup run.

**Parameters**
- `webhookUrl` – Destination URL. If empty or `null`, a warning is logged and the method returns without sending.
- `result` – The backup result. Sent fields: `Id`, `ScheduleId`, `Status`, `BackupFilePath`, `BackupFileSizeBytes`, `Checksum`, `StartedAt`, `CompletedAt`, `ErrorMessage`.
- `cancellationToken` – Cancels the request and any pending retry delay.

**Return**
A `Task` that completes after the send attempt (including retries) finishes.

**Throws**
`OperationCanceledException` if `cancellationToken` is cancelled. Other transport or serialization errors are logged and not thrown.

**Payload shape**

```json
{
  "eventType": "backup.completed",
  "timestamp": "2026-10-25T02:00:03.1234567Z",
  "data": {
    "Id": "…",
    "ScheduleId": "…",
    "Status": 0,
    "BackupFilePath": "/backups/app-20261025.db.gz",
    "BackupFileSizeBytes": 1048576,
    "Checksum": "…",
    "StartedAt": "…",
    "CompletedAt": "…",
    "ErrorMessage": null
  }
}
```

### Task SendScheduleNotificationAsync(string webhookUrl, BackupSchedule schedule, string eventType, CancellationToken cancellationToken = default)

**Purpose**
Sends a schedule-related event (for example, `schedule.created`) with a summary of the schedule.

**Parameters**
- `webhookUrl` – Destination URL.
- `schedule` – The backup schedule. Sent fields: `Id`, `Name`, `DatabasePath`, `CronExpression`, `IsEnabled`.
- `eventType` – Value written to the payload's `eventType` field. Not validated.
- `cancellationToken` – Cancels the request and any pending retry delay.

**Return**
A `Task` that completes after the send attempt (including retries) finishes.

**Throws**
`OperationCanceledException` if `cancellationToken` is cancelled. Other errors are logged and not thrown.

**Notes**
Unlike `SendBackupNotificationAsync`, this method does not check `webhookUrl` for empty values before posting. Passing an empty URL results in a logged request failure instead of a skipped send.

## Verifying Signatures (Receiver Side)

Receivers should:

1. Parse `X-Signature` into `t` (timestamp) and `v1` (signature).
2. Reject requests whose timestamp is more than about 5 minutes from the current time.
3. Recompute HMAC-SHA256 over the raw request body using the shared secret.
4. Compare the result with `v1` using a constant-time comparison.

Verify against the raw body bytes as received. Re-serializing the parsed JSON can change whitespace and property order, which breaks the signature.

## Usage Example

```csharp
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Integration;
using Microsoft.Extensions.Logging;

// Option 1: explicit secret and retry limit
var client = new WebhookClient(logger, maxRetries: 5, webhookSecret: "my-shared-secret");

// Option 2: take the secret from configuration (AppSettings.WebhookSecret)
var clientFromSettings = new WebhookClient(logger, settings, maxRetries: 3);

// Send a backup-completed event
await client.SendBackupNotificationAsync(
    "https://hooks.example.com/backups",
    backupResult,
    cancellationToken);

// Send a schedule event
await client.SendScheduleNotificationAsync(
    "https://hooks.example.com/backups",
    schedule,
    "schedule.created",
    cancellationToken);

// Signing a payload manually (for example, in a test or a custom sender)
string json = """{"eventType":"ping"}""";
string header = client.CreateSignatureHeader(json);
// header => "t=1761357603,v1=9f2c…"
```

Configure the secret via environment variables with the `BACKUP_` prefix, for example `BACKUP_AppSettings__WebhookSecret`.
