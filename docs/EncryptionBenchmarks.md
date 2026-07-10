# EncryptionBenchmarks

Provides a simple benchmark harness for measuring the performance of symmetric encryption and decryption operations in the `docker-sqlite-backup` project. The class encapsulates setup, teardown, and the core async operations used to evaluate cryptographic throughput.

## API

### `public void Setup()`
- **Purpose:** Initializes any required resources (e.g., cryptographic keys, buffers) before running encryption or decryption benchmarks.
- **Parameters:** None.
- **Return value:** None.
- **Exceptions:** 
  - `InvalidOperationException` if `Setup` is called more than once without an intervening `Cleanup`.
  - `ObjectDisposedException` if the underlying resources have already been disposed.

### `public void Cleanup()`
- **Purpose:** Releases resources allocated by `Setup`, returning the object to a clean state.
- **Parameters:** None.
- **Return value:** None.
- **Exceptions:** 
  - `ObjectDisposedException` if `Cleanup` is called after resources have already been released.
  - `InvalidOperationException` if `Cleanup` is invoked prior to a successful `Setup`.

### `public async Task Encrypt()`
- **Purpose:** Performs a single encryption operation using the state prepared by `Setup`. Intended for measuring encryption latency and throughput.
- **Parameters:** None.
- **Return value:** A `Task` that completes when the encryption operation finishes.
- **Exceptions:** 
  - `CryptographicException` if the encryption process fails (e.g., invalid key, algorithm error).
  - `InvalidOperationException` if called before `Setup` or after `Cleanup`.

### `public async Task Decrypt()`
- **Purpose:** Performs a single decryption operation on data previously encrypted by `Encrypt`, using the same prepared state.
- **Parameters:** None.
- **Return value:** A `Task` that completes when the decryption operation finishes.
- **Exceptions:** 
  - `CryptographicException` if decryption fails (e.g., corrupted data, incorrect key).
  - `InvalidOperationException` if called before `Setup` or after `Cleanup`.

## Usage

```csharp
using System.Threading.Tasks;

var bench = new EncryptionBenchmarks();
bench.Setup();
try
{
    await bench.Encrypt();   // Measure encryption performance
    await bench.Decrypt();   // Measure decryption performance
}
finally
{
    bench.Cleanup();        // Ensure resources are released
}
```

```csharp
using System.Threading.Tasks;

// Example of reusing the benchmark instance for multiple iterations
var bench = new EncryptionBenchmarks();
bench.Setup();

for (int i = 0; i < 1000; i++)
{
    await bench.Encrypt();
    await bench.Decrypt();
}

bench.Cleanup();
```

## Notes

- The class is **not thread-safe**; concurrent calls to `Setup`, `Cleanup`, `Encrypt`, or `Decrypt` from multiple threads may result in undefined behavior or exceptions. Use a single instance per thread or synchronize access externally.
- `Encrypt` and `Decrypt` rely on state initialized by `Setup`. Invoking either method before `Setup` or after `Cleanup` will throw an `InvalidOperationException`.
- Repeated calls to `Setup` without an intervening `Cleanup` are prohibited and will throw an `InvalidOperationException` to prevent resource leaks.
- `Cleanup` may be safely called multiple times only if the implementation guards against double disposal; however, the current contract treats a second `Cleanup` after resources have been released as an error (`ObjectDisposedException`).
- Cryptographic exceptions (`CryptographicException`) indicate failures in the underlying encryption algorithm and should be treated as fatal for the benchmark iteration.
