# EncryptionUtility

A utility class that provides asynchronous file encryption and decryption using AES symmetric encryption, along with helper methods for key generation and validation. Designed for scenarios requiring secure file storage or transmission, such as backup operations in containerized environments.

## API

### `public static async Task EncryptFileAsync(string inputFilePath, string outputFilePath, string base64Key)`

Encrypts the contents of a file at `inputFilePath` and writes the encrypted data to `outputFilePath` using AES encryption.

- **Parameters**
  - `inputFilePath` (string): Path to the file to be encrypted.
  - `outputFilePath` (string): Path where the encrypted file will be written.
  - `base64Key` (string): Base64-encoded encryption key (must be 32 bytes when decoded).

- **Return Value**
  - `Task`: A task representing the asynchronous operation.

- **Exceptions**
  - `ArgumentNullException`: Thrown if `inputFilePath`, `outputFilePath`, or `base64Key` is null.
  - `ArgumentException`: Thrown if `base64Key` is not a valid Base64 string or does not decode to 32 bytes.
  - `FileNotFoundException`: Thrown if `inputFilePath` does not exist.
  - `UnauthorizedAccessException`: Thrown if the caller lacks permissions to read `inputFilePath` or write to `outputFilePath`.

---

### `public static async Task DecryptFileAsync(string inputFilePath, string outputFilePath, string base64Key)`

Decrypts the contents of a file at `inputFilePath` and writes the decrypted data to `outputFilePath` using AES decryption.

- **Parameters**
  - `inputFilePath` (string): Path to the encrypted file.
  - `outputFilePath` (string): Path where the decrypted file will be written.
  - `base64Key` (string): Base64-encoded decryption key (must match the key used for encryption).

- **Return Value**
  - `Task`: A task representing the asynchronous operation.

- **Exceptions**
  - `ArgumentNullException`: Thrown if `inputFilePath`, `outputFilePath`, or `base64Key` is null.
  - `ArgumentException`: Thrown if `base64Key` is not a valid Base64 string or does not decode to 32 bytes.
  - `FileNotFoundException`: Thrown if `inputFilePath` does not exist.
  - `UnauthorizedAccessException`: Thrown if the caller lacks permissions to read `inputFilePath` or write to `outputFilePath`.
  - `CryptographicException`: Thrown if the file is corrupted, the key is incorrect, or decryption fails.

---
### `public static string GenerateBase64Key()`

Generates a cryptographically secure 256-bit (32-byte) AES key and returns it as a Base64-encoded string.

- **Return Value**
  - `string`: A Base64-encoded 256-bit AES key.

- **Exceptions**
  - None. The operation is guaranteed to succeed.

---
### `public static bool IsValidKey(string base64Key)`

Validates whether a given Base64-encoded string represents a valid 256-bit AES key.

- **Parameters**
  - `base64Key` (string): The Base64-encoded key to validate.

- **Return Value**
  - `bool`: `true` if the key is valid; otherwise, `false`.

- **Exceptions**
  - None.

## Usage

### Encrypting a file before backup
