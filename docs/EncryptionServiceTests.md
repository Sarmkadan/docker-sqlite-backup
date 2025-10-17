# EncryptionServiceTests

Unit tests for the `EncryptionService` class, verifying encryption/decryption functionality, key generation, validation, and service state management.

## API

### `public EncryptionServiceTests`

Initializes a new instance of the test class with required test dependencies.

### `public Task InitializeAsync()`

Configures the test environment before each test execution. Typically initializes mock services or test state.

### `public Task DisposeAsync()`

Cleans up resources after each test execution. Typically resets mocks or test state.

### `public void GenerateKey_ReturnsValidBase64Key()`

Verifies that `EncryptionService.GenerateKey()` returns a key that is a valid Base64 string.

### `public void GenerateKey_EachCallReturnsUniqueKey()`

Ensures that successive calls to `EncryptionService.GenerateKey()` produce distinct keys.

### `public void ValidateKey_ValidKey_ReturnsTrue()`

Confirms that `EncryptionService.ValidateKey(key)` returns `true` for a properly formatted encryption key.

### `public void ValidateKey_InvalidInputs_ReturnsFalse()`

Validates that `EncryptionService.ValidateKey(key)` returns `false` for malformed or invalid keys.

### `public void IsEncryptionEnabled_WhenDisabled_ReturnsFalse()`

Checks that `EncryptionService.IsEncryptionEnabled` returns `false` when encryption is disabled.

### `public void IsEncryptionEnabled_WhenEnabled_ReturnsTrue()`

Checks that `EncryptionService.IsEncryptionEnabled` returns `true` when encryption is enabled.

### `public void GetStatus_WhenDisabledAndNoKey_ReturnsDisabledStatus()`

Ensures that `EncryptionService.GetStatus()` returns a status indicating encryption is disabled when no key is present.

### `public void GetStatus_WhenEnabledWithValidKey_ReturnsActiveStatus()`

Verifies that `EncryptionService.GetStatus()` returns an active status when encryption is enabled and a valid key exists.

### `public async Task EncryptFileAsync_ThenDecryptFileAsync_RoundTripProducesOriginalContent()`

Tests that a file can be encrypted and then decrypted to produce the original content.

### `public async Task EncryptFileAsync_EncryptedFileIsDifferentFromPlaintext()`

Confirms that the encrypted output of `EncryptFileAsync` differs from the original plaintext file.

### `public async Task EncryptFileAsync_WhenEncryptionDisabled_ThrowsInvalidOperationException()`

Validates that `EncryptFileAsync` throws an `InvalidOperationException` when encryption is disabled.

### `public async Task EncryptFileAsync_NonExistentSource_ThrowsFileNotFoundException()`

Ensures that `EncryptFileAsync` throws a `FileNotFoundException` when the source file does not exist.

### `public async Task GetActiveKey_WhenEnabledWithValidKey_ReturnsKey()`

Verifies that `EncryptionService.GetActiveKey()` returns the active encryption key when encryption is enabled.

### `public void GetActiveKey_WhenDisabled_ReturnsNull()`

Confirms that `EncryptionService.GetActiveKey()` returns `null` when encryption is disabled.

## Usage
