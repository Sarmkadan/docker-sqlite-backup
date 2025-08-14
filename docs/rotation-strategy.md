# Backup Rotation Strategy Guide

This guide explains how to configure backup rotation policies to automatically remove old backups and manage storage usage.

## Overview

Rotation policies control when and how old backup files are deleted. Every schedule can have its own rotation policy. The service evaluates the policy after each successful backup and deletes files that no longer need to be kept.

There are four rotation strategies:

| Strategy | Description |
|---|---|
| `MaxFileCount` | Keep the N most recent backups and delete the rest |
| `MaxAge` | Delete backups older than N days |
| `Combined` | Delete backups that exceed the count **or** age limit (whichever triggers first) |
| `NoRotation` | Never delete backups automatically |

---

## Configuration Reference

Rotation policies are configured per schedule via the API or in `appsettings.json`. The full set of options:

| Field | Type | Default | Description |
|---|---|---|---|
| `Strategy` | int | `2` (Combined) | Rotation strategy (0=MaxFileCount, 1=MaxAge, 2=Combined, 3=NoRotation) |
| `MaxBackupCount` | int | `10` | Maximum number of backups to retain. Set to `0` for unlimited. |
| `MaxAgeDays` | int | `30` | Maximum age in days before a backup is eligible for deletion. |
| `MinimumBackupCount` | int | `3` | Backups to always keep regardless of age or count. |
| `DeleteFailedBackups` | bool | `true` | Whether to include failed backups in rotation. |
| `VerifyBeforeDeletion` | bool | `false` | Run integrity verification before deleting a backup. |

---

## Strategy Examples

### MaxFileCount — Keep the last N backups

Keeps the 5 most recent successful backups and deletes the rest. At least 2 are always retained.

```json
{
  "Strategy": 0,
  "MaxBackupCount": 5,
  "MinimumBackupCount": 2,
  "DeleteFailedBackups": true
}
```

**When to use:** Predictable storage usage when backup frequency is fixed.

---

### MaxAge — Delete backups older than N days

Keeps any backup made in the last 14 days. At least 3 are always retained regardless of age.

```json
{
  "Strategy": 1,
  "MaxAgeDays": 14,
  "MinimumBackupCount": 3,
  "DeleteFailedBackups": true
}
```

**When to use:** Compliance requirements that define retention periods by calendar time.

---

### Combined — Count and age limits (recommended)

Deletes a backup when it is either older than 30 days **or** when more than 10 backups exist. The `MinimumBackupCount` floor ensures at least 3 backups survive even if all of them are old.

```json
{
  "Strategy": 2,
  "MaxBackupCount": 10,
  "MaxAgeDays": 30,
  "MinimumBackupCount": 3,
  "DeleteFailedBackups": true
}
```

**When to use:** General-purpose default. Protects against both unbounded growth and stale backups.

---

### NoRotation — Keep all backups

Disables automatic deletion entirely. Backups must be removed manually or via the API.

```json
{
  "Strategy": 3,
  "MinimumBackupCount": 1
}
```

**When to use:** Archival schedules where every backup must be retained indefinitely. Ensure sufficient storage is provisioned.

---

## MinimumBackupCount Safety Floor

`MinimumBackupCount` acts as a hard floor. Rotation never deletes backups until the total count exceeds this number, regardless of the chosen strategy. This prevents accidental data loss when all recent backups happen to be old (for example, after a long pause in activity).

Example: with `MaxAgeDays: 7` and `MinimumBackupCount: 3`, if you have 3 backups all older than 7 days, none are deleted.

---

## Handling Failed Backups

When `DeleteFailedBackups: true` (the default), failed backups are removed before the count/age limits are applied. This keeps storage usage predictable and avoids polluting the backup history with unusable files.

Set `DeleteFailedBackups: false` if you need to keep failed backups for post-mortem diagnosis.

---

## Managing Rotation via the API

```bash
# Get the current rotation policy for a schedule
GET /api/schedules/{scheduleId}/rotation

# Create or update the rotation policy
PUT /api/schedules/{scheduleId}/rotation
Content-Type: application/json

{
  "strategy": 2,
  "maxBackupCount": 10,
  "maxAgeDays": 30,
  "minimumBackupCount": 3,
  "deleteFailedBackups": true
}

# Preview which backups would be deleted without actually deleting them
GET /api/schedules/{scheduleId}/rotation/preview
```

---

## Troubleshooting

**Backups are not being deleted**
- Confirm the schedule has a rotation policy assigned.
- Check that `Strategy` is not `3` (NoRotation).
- Verify that the total backup count exceeds `MinimumBackupCount`.
- Review application logs for `Rotation completed` messages after each backup run.

**Too many backups are being deleted**
- Increase `MinimumBackupCount` to preserve more recent backups unconditionally.
- Switch from `Combined` to `MaxAge` if count-based deletion is too aggressive.

**Storage is still growing despite rotation**
- Set `DeleteFailedBackups: true` to clean up failed-backup artifacts.
- Reduce `MaxBackupCount` or `MaxAgeDays` to match your available storage budget.
