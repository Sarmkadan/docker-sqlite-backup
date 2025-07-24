# Changelog

## [2.0.2] - 2026-05-21
- Fix connection string parsing when database path contains spaces or unicode characters

## [2.0.0] - 2026-03-15
- Add AES-256-CBC encryption for backup files (key via `BACKUP_ENCRYPTION_KEY` env var)
- Add Azure Blob Storage backend
- Add incremental backup mode using WAL checkpoint
- Add integrity checker with three-level SQLite scan (quick/full/foreign-key)
- Add restore verification with record-count comparison

## [1.2.0] - 2025-11-10
- Add S3 server-side encryption and configurable storage classes
- Add SHA-256 checksum validation on verify

## [1.0.0] - 2025-08-01
- Initial release: cron-based scheduling, local and S3 storage, rotation, restore verification
