# ChecksumUtility

A utility class providing static methods to compute various checksums and hashes for files, strings, and collections, commonly used for data integrity verification in backup and synchronization workflows.

## API

### `CalculateFileSha256Async`
Computes the SHA-256 hash of a file asynchronously.

- **Parameters**
  - `filePath` (string): The path to the file to hash.
- **Returns**
  - `Task<string>`: A hexadecimal string representing the SHA-256 hash of the file contents.
- **Exceptions**
  - Throws `ArgumentNullException` if `filePath` is null.
  - Throws `FileNotFoundException` if the file does not exist.
  - Throws `IOException` if the file cannot be read.

### `CalculateStringSha256`
Computes the SHA-256 hash of a string.

- **Parameters**
  - `input` (string): The string to hash.
- **Returns**
  - `string`: A hexadecimal string representing the SHA-256 hash of the input.
- **Exceptions**
  - Throws `ArgumentNullException` if `input` is null.

### `CalculateFileMd5Async`
Computes the MD5 hash of a file asynchronously.

- **Parameters**
  - `filePath` (string): The path to the file to hash.
- **Returns**
  - `Task<string>`: A hexadecimal string representing the MD5 hash of the file contents.
- **Exceptions**
  - Throws `ArgumentNullException` if `filePath` is null.
  - Throws `FileNotFoundException` if the file does not exist.
  - Throws `IOException` if the file cannot be read.

### `VerifyFileSha256Async`
Verifies that the SHA-256 hash of a file matches an expected value.

- **Parameters**
  - `filePath` (string): The path to the file to verify.
  - `expectedHash` (string): The expected SHA-256 hash in hexadecimal format.
- **Returns**
  - `Task<bool>`: `true` if the computed hash matches `expectedHash`; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `filePath` or `expectedHash` is null.
  - Throws `FileNotFoundException` if the file does not exist.
  - Throws `IOException` if the file cannot be read.
  - Throws `FormatException` if `expectedHash` is not a valid hexadecimal string.

### `CalculateFileCrc32Async`
Computes the CRC-32 checksum of a file asynchronously.

- **Parameters**
  - `filePath` (string): The path to the file to checksum.
- **Returns**
  - `Task<uint>`: The CRC-32 checksum as an unsigned 32-bit integer.
- **Exceptions**
  - Throws `ArgumentNullException` if `filePath` is null.
  - Throws `FileNotFoundException` if the file does not exist.
  - Throws `IOException` if the file cannot be read.

### `GenerateQuickChecksum`
Generates a quick checksum for a string using a non-cryptographic hash.

- **Parameters**
  - `input` (string): The string to checksum.
- **Returns**
  - `string`: A hexadecimal string representing the quick checksum.
- **Exceptions**
  - Throws `ArgumentNullException` if `input` is null.

### `GenerateQuickChecksumAsync`
Generates a quick checksum for a file asynchronously using a non-cryptographic hash.

- **Parameters**
  - `filePath` (string): The path to the file to checksum.
- **Returns**
  - `Task<string>`: A hexadecimal string representing the quick checksum.
- **Exceptions**
  - Throws `ArgumentNullException` if `filePath` is null.
  - Throws `FileNotFoundException` if the file does not exist.
  - Throws `IOException` if the file cannot be read.

### `CalculateCollectionChecksum`
Computes a combined checksum for a collection of strings.

- **Parameters**
  - `items` (IEnumerable<string>): The collection of strings to checksum.
- **Returns**
  - `string`: A hexadecimal string representing the combined checksum.
- **Exceptions**
  - Throws `ArgumentNullException` if `items` is null.

## Usage
