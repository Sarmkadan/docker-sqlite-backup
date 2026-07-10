# IntegrityReport

A lightweight data structure that captures the results of SQLite database integrity checks performed by `docker-sqlite-backup`. It records metadata about the database file, the outcome of quick and full integrity checks, and key storage metrics such as page counts and journaling mode. This type is typically produced by backup or maintenance tools to verify the structural health of a SQLite database before or after operations.

## API

### `Id`
A unique identifier for this integrity report. This value is generated when the report is created and remains constant for the lifetime of the report.

### `DatabasePath`
The absolute file system path to the SQLite database that was checked. This path is captured at the time of the integrity check and reflects the location of the database file on disk.

### `CheckedAt`
The timestamp indicating when the integrity check was initiated. This value is set at the beginning of the check process and is not updated during execution.

### `Duration`
The total elapsed time taken to perform the integrity checks, measured from start to completion. This duration includes both quick and full checks, if performed.

### `PassedQuickCheck`
A boolean indicating whether the quick integrity check completed successfully. A value of `true` means no structural issues were detected during the quick check phase.

### `QuickCheckErrors`
A string containing any error messages generated during the quick integrity check, if applicable. If no errors occurred, this field is `null`. The content is typically a concatenation of SQLite error messages or diagnostic output.

### `PassedFullCheck`
A boolean indicating whether the full integrity check completed successfully. A value of `true` means the database passed all validation rules during the comprehensive check phase.

### `FullCheckErrors`
A string containing any error messages generated during the full integrity check, if applicable. If no errors occurred, this field is `null`. This typically includes detailed output from SQLite’s `PRAGMA integrity_check` command.

### `PassedForeignKeyCheck`
A boolean indicating whether the foreign key constraint check completed successfully. A value of `true` means all foreign key relationships in the database are valid and enforceable.

### `ForeignKeyErrors`
A string containing any error messages generated during the foreign key constraint check, if applicable. If no errors occurred, this field is `null`. This usually includes details about violated foreign key constraints or missing referenced rows.

### `PageCount`
The total number of pages in the database file. This value reflects the current size of the database in terms of SQLite’s page-based storage model.

### `PageSize`
The size, in bytes, of each page in the database. This value is typically 4096 or another power-of-two value consistent with filesystem block sizes.

### `FreePageCount`
The number of unused (free) pages in the database file. This indicates how much space could potentially be reclaimed or reused without shrinking the database.

### `JournalMode`
The current journaling mode of the database (e.g., `DELETE`, `TRUNCATE`, `PERSIST`, `MEMORY`, `WAL`). This value reflects how SQLite is configured to handle crash recovery and concurrent access.

### `HasUncheckpointedWal`
A boolean indicating whether the Write-Ahead Log (WAL) mode is active and contains uncheckpointed frames. A value of `true` means there are pending WAL frames that have not been written back to the main database file.

### `TableCount`
The total number of tables (including system and user-defined) present in the database at the time of the check. This count reflects the schema state during the integrity verification.

## Usage
