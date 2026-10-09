# EncryptionService

`EncryptionService` (`src/Services/EncryptionService.cs`) encrypts and decrypts backup files with AES-256-CBC. It implements `IEncryptionService`, resolves the key from the environment or application settings, and reports the encryption configuration through `EncryptionStatus`.

The actual file I/O is delegated to `EncryptionUtility` (`src/Utilities`). This class adds key resolution, configuration checks, logging, and cancellation checks on top of it.

## Dependencies

| Dependency | Purpose |
|---|---|
| `AppSettings` | Provides `EnableEncryption` and `EncryptionKey` (bound from `appsettings.json` or `BACKUP_` env vars). |
| `ILogger<EncryptionService>` | Logs warnings, errors, and success messages. |
| `EncryptionUtility` | Performs the AES-256-CBC stream operations, key generation, and key validation. |

## Constructor

```csharp
public EncryptionService(AppSettings settings, ILogger<EncryptionService> logger)
```

Registered through dependency injection. The constructor only stores its arguments. It does not read the key.

## Key resolution

The key is read on each call, not cached at construction time.

1. Environment variable `BACKUP_ENCRYPTION_KEY`.
2. Otherwise `AppSettings.EncryptionKey` (env equivalent: `AppSettings__EncryptionKey`).

`ResolveKey()` returns `null` (and logs) in these cases:

- `AppSettings.EnableEncryption` is `false`. No warning is logged in this case.
- No key is set (empty or whitespace). Logs a warning.
- The key is not a valid Base64 value that decodes to 32 bytes. Logs an error. The key itself is never logged.

## Members

### `Task<string> EncryptFileAsync(string sourcePath, string destinationPath, CancellationToken ct = default)`

1. Throws `ArgumentException` if either path is null or whitespace.
2. If the source does not exist, logs a warning and throws `FileNotFoundException`.
3. Resolves the key. If there is none, throws `InvalidOperationException` ("Encryption key is not configured...").
4. Checks `ct`.
5. Calls `EncryptionUtility.EncryptFileAsync`. On success, logs at Information level. On failure, logs an error and rethrows.

Returns `destinationPath`.

Output format: `[16-byte random IV][AES-256-CBC ciphertext]`. A new IV is generated for each call.

### `Task<string> DecryptFileAsync(string sourcePath, string destinationPath, CancellationToken ct = default)`

1. Throws `ArgumentException` if either path is null or whitespace.
2. If the source does not exist, throws `FileNotFoundException`. Unlike encryption, it does not log a warning here.
3. Resolves the key. If there is none, throws `InvalidOperationException` ("Decryption key is not configured...").
4. Checks `ct`, then calls `EncryptionUtility.DecryptFileAsync`.
5. Logs at Information level.

Returns `destinationPath`.

Errors from `EncryptionUtility` propagate unchanged. For example, a file shorter than the 16-byte IV throws `InvalidDataException`. A wrong key or corrupted data typically surfaces as `CryptographicException` from the padding check. This method does not log failures.

### `string GenerateKey()`

Returns a new random 32-byte AES-256 key, Base64-encoded, via `EncryptionUtility.GenerateBase64Key()`. It does not store the key anywhere.

### `bool ValidateKey(string? base64Key)`

Returns `true` when the input is Base64 and decodes to exactly 32 bytes. Does not check whether the key matches any particular file.

### `bool IsEncryptionEnabled()`

Returns `AppSettings.EnableEncryption`. It does not check whether a key is present. Use `GetStatus()` for that.

### `string? GetActiveKey()`

Returns the resolved key, or `null` if encryption is disabled, no key is set, or the key is invalid.

Security note: this returns raw key material. Do not log it or include it in exception messages.

### `EncryptionStatus GetStatus()`

Builds a snapshot without throwing and without logging the key:

| Property | Meaning |
|---|---|
| `IsEnabled` | Value of `EnableEncryption`. |
| `HasValidKey` | `true` when the active key passes `ValidateKey`. |
| `KeyFingerprint` | First 8 hex characters (lowercase) of SHA-256 over the decoded key bytes. `null` when there is no valid key. |
| `KeySource` | `"Environment"`, `"Configuration"`, or `"None"`. |
| `Summary` | Human-readable text built from the other properties. |

Source precedence in `GetStatus()` matches `ResolveKey()`, with one difference. `GetStatus()` treats a whitespace-only env var as absent, but `ResolveKey()` uses `??`, so a whitespace-only `BACKUP_ENCRYPTION_KEY` makes `ResolveKey()` return `null` and does not fall back to the config key. `GetStatus()` would report the config key as the source.

## Usage

```csharp
// Generate a key once and store it as BACKUP_ENCRYPTION_KEY
var key = encryptionService.GenerateKey();

// Encrypt a backup
await encryptionService.EncryptFileAsync("/data/backups/db.sqlite", "/data/backups/db.sqlite.enc");

// Check configuration before a restore
var status = encryptionService.GetStatus();
if (!status.HasValidKey)
    throw new InvalidOperationException(status.Summary);

await encryptionService.DecryptFileAsync("/data/backups/db.sqlite.enc", "/restore/db.sqlite");
```

## Notes

- **File format**: `[16-byte IV][ciphertext]`. Files encrypted by other tools must use the same layout and key.
- **Destination overwrites**: the destination file is created or overwritten. An `IOException` is possible if it is locked or read-only.
- **Validation vs. decryption**: `ValidateKey` checks format only. A syntactically valid key can still fail to decrypt a file encrypted with a different key.
- **Thread safety**: the class holds no mutable state. Key resolution reads the environment and settings on each call.
