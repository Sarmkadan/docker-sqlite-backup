# EncryptionServiceTestsExtensions

Utility class providing static extension methods for testing `EncryptionService` functionality. These methods simplify the creation of test scenarios, validation of encryption states, and verification of encrypted content behavior.

## API

### `public static EncryptionService CreateEnabledService()`
Creates an `EncryptionService` instance with encryption enabled using default test settings. The service is configured to perform encryption operations during backup and decryption during restore.

- **Returns**: A new `EncryptionService` instance with encryption enabled.
- **Throws**: May throw if default configuration or service initialization fails.

---

### `public static EncryptionService CreateDisabledService()`
Creates an `EncryptionService` instance with encryption explicitly disabled. The service will skip encryption/decryption operations, treating data as plaintext.

- **Returns**: A new `EncryptionService` instance with encryption disabled.
- **Throws**: May throw if service initialization fails.

---

### `public static void ShouldBeEncryptionEnabled(EncryptionService service)`
Asserts that the given `EncryptionService` has encryption enabled.

- **Parameters**:
  - `service`: The `EncryptionService` instance to validate.
- **Throws**: Throws if encryption is not enabled or if the service is `null`.

---

### `public static void ShouldBeEncryptionDisabled(EncryptionService service)`
Asserts that the given `EncryptionService` has encryption disabled.

- **Parameters**:
  - `service`: The `EncryptionService` instance to validate.
- **Throws**: Throws if encryption is enabled or if the service is `null`.

---
### `public static string CreateTempFile()`
Creates a uniquely named temporary file in the system's temporary directory. The file is created but left empty.

- **Returns**: The full path to the created temporary file.
- **Throws**: May throw if temporary directory access fails or file creation is denied.

---
### `public static void ShouldExistAndNotBeEmpty(string filePath)`
Asserts that the specified file exists and has a non-zero length.

- **Parameters**:
  - `filePath`: The path to the file to validate.
- **Throws**:
  - Throws if the file does not exist.
  - Throws if the file exists but is empty.
  - Throws if the path is `null` or invalid.

---
### `public static async Task<string> EncryptToTempFileAsync(EncryptionService service, string plaintext)`
Encrypts the provided plaintext using the given `EncryptionService` and writes the result to a temporary file.

- **Parameters**:
  - `service`: The `EncryptionService` instance to use for encryption.
  - `plaintext`: The plaintext content to encrypt.
- **Returns**: A `Task<string>` resolving to the path of the temporary file containing the encrypted content.
- **Throws**:
  - Throws if `service` is `null`.
  - Throws if encryption fails.
  - Throws if file I/O fails.

---
### `public static async Task ShouldHaveDifferentContentAsync(string filePath1, string filePath2)`
Asserts that the contents of two files are not identical.

- **Parameters**:
  - `filePath1`: Path to the first file.
  - `filePath2`: Path to the second file.
- **Throws**:
  - Throws if either file does not exist.
  - Throws if the files contain identical content.
  - Throws if file I/O fails.

---
### `public static async Task ShouldRoundTripSuccessfullyAsync(EncryptionService service, string plaintext)`
Encrypts the given plaintext using the provided `EncryptionService`, then decrypts the result and verifies the original plaintext is restored.

- **Parameters**:
  - `service`: The `EncryptionService` instance to use for both operations.
  - `plaintext`: The original plaintext content to test.
- **Returns**: A `Task` that completes when the round-trip validation succeeds.
- **Throws**:
  - Throws if `service` is `null`.
  - Throws if encryption or decryption fails.
  - Throws if decrypted content does not match original plaintext.

## Usage

### Example 1: Validating encryption state and round-trip behavior
