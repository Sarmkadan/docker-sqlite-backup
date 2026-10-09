# Notification Channels

This document describes how docker-sqlite-backup sends notifications about backup events, which channels exist, which of them are wired in the running container, and what is planned but not implemented.

## Overview

Notifications are produced by `NotificationEventListener` (`src/Events/NotificationEventListener.cs`), an `IBackupEventListener` that reacts to backup events published by `BackupEventPublisher`. For each supported event it can:

1. Deliver a short text message through every registered `INotificationClient` (title + message).
2. Send a JSON webhook through `WebhookClient` (`src/Integration/WebhookClient.cs`), currently only for `backup.completed`.

## Channels

| Channel | Type | Status | Triggered by |
|---|---|---|---|
| Console | `ConsoleNotificationClient` (`src/Integration/NotificationClient.cs`) | Implemented, always active | All supported events |
| Webhook | `WebhookClient.SendBackupNotificationAsync` / `SendScheduleNotificationAsync` | Implemented, active only when a webhook URL is passed to the listener | `backup.completed` (backup payload); schedule payload method exists but is not called by the listener |
| Email | `AppSettings.NotificationEmails`, `BackupSchedule.NotificationEmails` | Not implemented. Addresses are validated only | None |
| Slack | none | Not implemented. The listener summary comment mentions Slack, but no client exists | None |

### Console

`ConsoleNotificationClient` is added in the `NotificationEventListener` constructor, so it is always present. It writes `[title] message` to standard output in yellow. Output goes to container logs (`docker logs`).

### Webhook

`WebhookClient` sends `POST` requests with `Content-Type: application/json`, a `User-Agent` of `docker-sqlite-backup/2.0`, and a 30 second timeout.

Payload shape for backup results:

```json
{
  "eventType": "backup.completed",
  "timestamp": "2026-10-09T12:00:00Z",
  "data": {
    "Id": "...",
    "ScheduleId": "...",
    "Status": "...",
    "BackupFilePath": "...",
    "BackupFileSizeBytes": 0,
    "Checksum": "...",
    "StartedAt": "...",
    "CompletedAt": "...",
    "ErrorMessage": null
  }
}
```

Signing: when a secret is configured (`BACKUP_WEBHOOK_SECRET` or `WebhookSecret` in `appsettings.json`), each request carries `X-Signature: t=<unix-seconds>,v1=<hex-hmac-sha256>`. See `docs/webhook-signature.md` for the verification procedure.

Retries: only `503 Service Unavailable` and `408 Request Timeout` are retried, up to `maxRetries` (default 3) with exponential delay (100 ms, 200 ms, 400 ms). Other non-success status codes are logged as errors and not retried. Network exceptions are logged and swallowed, except cancellation during shutdown, which is rethrown.

## Event Matrix

Events handled by `NotificationEventListener.HandleAsync`:

| Event type | Console title | Console message | Webhook |
|---|---|---|---|
| `backup.completed` | "Backup Completed" | Duration and backup size | Yes (if URL configured) |
| `backup.failed` | "Backup Failed" | Error message | No |
| `backup.retry` | "Backup Retry" | Attempt number (1st, 2nd, ...) | No |
| `schedule.created` | "Schedule Created" | Schedule name and cron expression | No |
| `restore.verification.completed` | "Verification Passed" or "Verification Failed" (depends on `IsValid`) | `ValidationMessage` or a generic text | No |
| `restore.verification.failed` | "Verification Failed" | Failure stage and exception message | No |

Event types are also exposed via `GetSupportedEventTypes()` and `CanHandle(eventType)`.

## Configuration

| Setting | Source | Effect |
|---|---|---|
| `BACKUP_WEBHOOK_SECRET` / `WebhookSecret` | env var / `appsettings.json` | Enables HMAC signing of webhook payloads |
| `NotificationEmails` | `appsettings.json` (`AppSettings`) and per-schedule `BackupSchedule.NotificationEmails` | Validated for format only. Not used for delivery |

No webhook URL setting exists in `AppSettings`, and no DI registration of `NotificationEventListener` or `WebhookClient` was found outside tests. In the current code, a webhook is sent only when the listener is constructed with a URL, for example by custom host code. Operators who expect webhooks from environment configuration should verify this before relying on it.

## Adding a Channel

1. Implement `INotificationClient` (`SendAsync(title, message, ct)` and `IsConfigured()`).
2. Register it with `NotificationEventListener.AddNotificationClient(client)` or the `AddNotificationClients` extensions (`docs/NotificationEventListenerExtensions.md`).
3. Clients are invoked in parallel via `Task.WhenAll`. A client that throws will fail the whole notification batch for that event, so client implementations should catch and log their own errors.

## Known Gaps

- Email delivery is not implemented. `NotificationEmails` is validated but never sent.
- Slack is not implemented.
- Webhooks are sent only for `backup.completed`, not for failure or verification events.
- `SendScheduleNotificationAsync` exists but has no caller in the listener.
- Webhook configuration is not bound from settings in the default host.
