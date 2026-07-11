# ChecksumUtilityTests

The `ChecksumUtilityTests` class contains unit tests that verify the correctness and robustness of checksum calculation utilities within the `docker-sqlite-backup` project. These tests ensure that various hash algorithms (SHA-256, MD5, CRC32) and checksum generation methods produce deterministic, consistent results for files, strings, and collections, while also validating error handling for edge cases such as non-existent files.

## API

### `InitializeAsync`
**Purpose**: Initializes test resources before execution. Typically used to set up temporary files or test environments.
**Parameters**: None.
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: Dependent on implementation; may throw exceptions during setup.

### `DisposeAsync`
**Purpose**: Cleans up test resources after execution. Ensures temporary files or test artifacts are removed.
**Parameters**: None.
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: Dependent on implementation; may throw exceptions during teardown.

### `CalculateFileSha256Async_KnownContent_ReturnsDeterministicHash`
**Purpose**: Verifies that `CalculateFileSha256Async` produces the same hash for a file with known content across multiple invocations.
**Parameters**: None.
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: Fails the test if the hash is not deterministic.

### `CalculateFileSha256Async_DifferentContent_ReturnsDifferentHash`
**Purpose**: Ensures that `CalculateFileSha256Async` generates different hashes for files with differing content.
**Parameters**: None.
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: Fails the test if identical hashes are returned for different content.

### `CalculateFileSha256Async_NonExistentFile_ThrowsFileNotFoundException`
**Purpose**: Confirms that `CalculateFileSha256Async` throws a `FileNotFoundException` when attempting to hash a non-existent file.
**Parameters**: None.
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: Fails the test if no exception is thrown or if the exception type is incorrect.

### `CalculateStringSha256_KnownInput_ReturnsDeterministicHash`
**Purpose**: Validates that `CalculateStringSha256` returns the same hash for a known input string.
**Parameters**: None.
**Return Value**: Void.
**Throws**: Fails the test if the hash is not deterministic.

### `CalculateStringSha256_DifferentInputs_ReturnsDifferentHashes`
**Purpose**: Ensures that `CalculateStringSha256` produces different hashes for different input strings.
**Parameters**: None.
**Return Value**: Void.
**Throws**: Fails the test if identical hashes are returned for different inputs.

### `VerifyFileSha256Async_CorrectHash_ReturnsTrue`
**Purpose**: Tests that `VerifyFileSha256Async` returns `true` when comparing a file's hash against its correct precomputed hash.
**Parameters**: None.
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: Fails the test if the result is not `true`.

### `VerifyFileSha256Async_WrongHash_ReturnsFalse`
**Purpose**: Verifies that `VerifyFileSha256Async` returns `false` when comparing a file's hash against an incorrect hash.
**Parameters**: None.
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: Fails the test if the result is not `false`.

### `VerifyFileSha256Async_HashIsCaseInsensitive_ReturnsTrue`
**Purpose**: Confirms that `VerifyFileSha256Async` treats hash comparisons as case-insensitive, returning `true` for matching hashes regardless of case.
**Parameters**: None.
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: Fails the test if the comparison is case-sensitive.

### `CalculateFileMd5Async_KnownContent_ReturnsDeterministicHash`
**Purpose**: Ensures that `CalculateFileMd5Async` produces the same hash for a file with known content.
**Parameters**: None.
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: Fails the test if the hash is not deterministic.

### `CalculateFileMd5Async_NonExistentFile_ThrowsFileNotFoundException`
**Purpose**: Validates that `CalculateFileMd5Async` throws a `FileNotFoundException` for non-existent files.
**Parameters**: None.
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: Fails the test if no exception is thrown or if the exception type is incorrect.

### `CalculateCollectionChecksum_SameValues_ReturnsSameHash`
**Purpose**: Tests that `CalculateCollectionChecksum` returns the same hash for collections with identical values.
**Parameters**: None.
**Return Value**: Void.
**Throws**: Fails the test if the hash is not deterministic.

### `CalculateCollectionChecksum_DifferentValues_ReturnsDifferentHash`
**Purpose**: Ensures that `CalculateCollectionChecksum` produces different hashes for collections with differing values.
**Parameters**: None.
**Return Value**: Void.
**Throws**: Fails the test if identical hashes are returned for different collections.

### `GenerateQuickChecksumAsync_SameFile_ReturnsSameChecksum`
**Purpose**: Verifies that `GenerateQuickChecksumAsync` generates the same checksum for the same file across multiple invocations.
**Parameters**: None.
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: Fails the test if the checksum is not deterministic.

### `GenerateQuickChecksumAsync_NonExistentFile_ThrowsFileNotFoundException`
**Purpose**: Confirms that `GenerateQuickChecksumAsync` throws a `FileNotFoundException` for non-existent files.
**Parameters**: None.
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: Fails the test if no exception is thrown or if the exception type is incorrect.

### `CalculateFileCrc32Async_KnownContent_ReturnsDeterministicResult`
**Purpose**: Ensures that `CalculateFileCrc32Async` returns the same CRC32 result for a file with known content.
**Parameters**: None.
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: Fails the test if the result is not deterministic.

### `CalculateFileCrc32Async_NonExistentFile_ThrowsFileNotFoundException`
**Purpose**: Validates that `CalculateFileCrc32Async` throws a `FileNotFoundException` for non-existent files.
**Parameters**: None.
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: Fails the test if no exception is thrown or if the exception type is incorrect.

## Usage

### Example 1: Verifying File Integrity with SHA-256
