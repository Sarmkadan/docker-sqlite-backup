# Restore Guide

This guide describes how to restore a database from a backup produced by docker-sqlite-backup. The tool has no built-in restore command. Restore is a manual procedure: take the backup file, undo the optional encryption and compression steps, and place the resulting SQLite file where the application expects it.

## Backup file formats

Each backup is written by `BackupService` through a fixed pipeline:

1. SQLite snapshot is written as `backup_<timestamp>.sqlite` (or `backup_<schedule>_<timestamp>.sqlite` for schedules).
2. If `EncryptionKey` is configured, the file is encrypted with AES-256 and renamed to `<name>.enc`. The original plaintext file is deleted.
3. If `CompressBackups` is `true`, the file is gzipped and `.gz` is appended.

Resulting file names depend on which options were enabled:

| Settings | Resulting file | Restore steps |
|---|---|---|
| Neither | `backup_*.sqlite` | Copy file as-is |
| Compression only | `backup_*.sqlite.gz` | Gunzip |
| Encryption only | `backup_*.sqlite.enc` | Decrypt |
| Both | `backup_*.sqlite.enc.gz` | Gunzip, then decrypt |

The checksum stored in the backup metadata (`BackupResult.Checksum`, and `.meta.json` next to the backup) is computed over the final file. Verify it before restoring if you need to rule out corruption in transit.

## Encryption format

- Algorithm: AES-256, key size 256 bits.
- The first 16 bytes of the `.enc` file are the IV. The rest is ciphertext.
- The key is a Base64-encoded 32-byte value. It is read from `BACKUP_ENCRYPTION_KEY` or `AppSettings__EncryptionKey`.

Without the exact key that produced the backup, the file cannot be decrypted. Keep the key in a secret store separate from the backup storage.

## Restore procedure

Stop the backup container before replacing any database file, so no backup or write runs against it during restore.

```bash
docker compose stop
```

`<target.sqlite>` below is the database you are restoring to. It is not necessarily the same file that is backed up (`DatabasePath`, default `backups.sqlite`). Set it explicitly before running the commands.

### 1. Select the backup

Backups are stored in the local storage path (`LocalStoragePath`, default `backups`, mounted to `./backups` in `docker-compose.yml`). Pick a file by timestamp in its name. For S3 or Azure, download the object first.

### 2. Keep a copy of the current target database

```bash
cp <target.sqlite> <target.sqlite>.pre-restore
```

Skip this step only if you are certain the current file is unusable.

### 3. Decrypt and/or decompress

Gunzip only (compression, no encryption):

```bash
gunzip -c backups/backup_2026-10-01_02-00-00.sqlite.gz > restored.sqlite
```

Decrypt only (encryption, no compression). The key is passed through the environment and must not be written to shell history:

```bash
BACKUP_ENCRYPTION_KEY='<base64-key>' \
  openssl enc -d -aes-256-cbc -K "$(printf '%s' "$BACKUP_ENCRYPTION_KEY" | base64 -d | xxd -p -c 64)" \
  -iv "$(head -c 16 backups/backup.sqlite.enc | xxd -p -c 32)" \
  -in <(tail -c +17 backups/backup.sqlite.enc) -out restored.sqlite
```

Both (encryption and compression): decompress first, then decrypt the result.

```bash
gunzip -c backups/backup.sqlite.enc.gz > backup.sqlite.enc
# then apply the decrypt step above to backup.sqlite.enc
```

Check that the restored file is a valid SQLite database:

```bash
sqlite3 restored.sqlite "PRAGMA integrity_check;"
```

The expected output is `ok`.

### 4. Replace the database

```bash
mv restored.sqlite <target.sqlite>
```

### 5. Start the container

```bash
docker compose up -d
```

Check the health status file (`HealthCheckStatusFilePath`, default `health-status.json`) and the container logs after startup.

## Verifying a backup before restore

`VerificationService` runs the same steps as above into a temporary directory, then runs `PRAGMA integrity_check` and counts records. It is controlled by `EnableVerificationByDefault`. Results are stored as `RestoreVerification` records and emitted as events.

Known limitation: `VerificationService.RestoreToTemporaryAsync` checks the file extension and handles only one layer. A file ending in `.enc` is decrypted, and a file ending in `.gz` is gunzipped. A file ending in `.enc.gz` (both options enabled) matches only the `.gz` branch. The result is still encrypted, the integrity check fails, and the verification is recorded as failed. The backup itself is usable. Restore it with the manual steps above, and treat the failed verification as a bug in the verification step, not as a broken backup.

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| `Backup file is encrypted but no valid decryption key is configured` | `BACKUP_ENCRYPTION_KEY` is missing or not a valid 32-byte Base64 key |
| `Encrypted file is too short to contain a valid IV` | File is truncated or is not an encrypted backup |
| `gunzip: invalid compressed data` | Transfer was interrupted, or the file is an encrypted file with `.gz` appended by a different tool |
| `PRAGMA integrity_check` returns errors | Backup was taken from a corrupted source, or the wrong key was used (decryption produces garbage without an error) |
| Verification fails on a `.enc.gz` file | Known limitation above |

Wrong keys do not produce a clear error during decryption. A wrong key yields a file that fails `integrity_check`. If this happens, confirm the key first.

## Related

- `docs/RestoreVerification.md` - verification result model
- `docs/EncryptionUtility.md` - encryption details
- `docs/webhook-signature.md` - webhook events emitted on verification
