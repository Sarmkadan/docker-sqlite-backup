# ChecksumBenchmarks

A benchmarking harness that measures the throughput and resource consumption of checksum computation strategies used in the `docker-sqlite-backup` project. It isolates the performance characteristics of SHA‑256, CRC‑32, and a lightweight quick‑checksum algorithm to inform runtime selection based on file size and I/O constraints.

## API

### `public void Setup()`
Prepares the benchmark environment before each iteration. Creates or reuses a temporary test file of a predetermined size so that every measurement operates on identical input data.  
*Parameters*: none.  
*Return value*: none.  
*Throws*: `IOException` when the temporary file cannot be created or written; `UnauthorizedAccessException` if the target directory is not writable.

### `public void Cleanup()`
Releases resources after each benchmark iteration. Deletes the temporary test file and suppresses any finalizer‑induced delays during teardown.  
*Parameters*: none.  
*Return value*: none.  
*Throws*: `IOException` when file deletion fails (e.g., a handle remains open); `UnauthorizedAccessException` if permissions are insufficient for deletion.

### `public async Task<string> CalculateSha256()`
Computes the SHA‑256 hash of the test file asynchronously, returning the lowercase hexadecimal digest string. Designed to represent the strongest integrity check available in the backup pipeline.  
*Parameters*: none.  
*Return value*: a 64‑character hexadecimal string (256‑bit hash).  
*Throws*: `FileNotFoundException` if the test file was removed before invocation; `IOException` on read failures; `ObjectDisposedException` if the underlying stream has already been closed.

### `public async Task<uint> CalculateCrc32()`
Computes the CRC‑32 checksum of the test file asynchronously, returning the unsigned 32‑bit integer result. Used to benchmark the fast corruption‑detection path.  
*Parameters*: none.  
*Return value*: a `uint` representing the CRC‑32 value.  
*Throws*: `FileNotFoundException` if the test file is missing; `IOException` on read failures; `ObjectDisposedException` if the underlying stream has already been closed.

### `public async Task<string> GenerateQuickChecksum()`
Produces a short, non‑cryptographic checksum string by sampling blocks of the test file. Intended for scenarios where minimal CPU overhead is paramount and a full hash is unnecessary.  
*Parameters*: none.  
*Return value*: a compact string (typically 8–16 characters) derived from sparse file regions.  
*Throws*: `FileNotFoundException` if the test file is absent; `IOException` on read failures; `ObjectDisposedException` if the underlying stream has already been closed.

## Usage

```csharp
// Example 1: Running all checksum benchmarks via BenchmarkDotNet
var summary = BenchmarkRunner.Run<ChecksumBenchmarks>();

// Inspect individual results
foreach (var report in summary.Reports)
{
    Console.WriteLine($"{report.BenchmarkCase.Descriptor.WorkloadMethod.Name}: "
                      + $"{report.ResultStatistics.Mean} ns");
}
```

```csharp
// Example 2: Manual invocation for ad‑hoc comparison
var bench = new ChecksumBenchmarks();
bench.Setup();

try
{
    string sha256 = await bench.CalculateSha256();
    uint crc32   = await bench.CalculateCrc32();
    string quick = await bench.GenerateQuickChecksum();

    Console.WriteLine($"SHA‑256: {sha256}");
    Console.WriteLine($"CRC‑32:  0x{crc32:X8}");
    Console.WriteLine($"Quick:   {quick}");
}
finally
{
    bench.Cleanup();
}
```

## Notes

- **Edge cases**: All methods assume the test file exists and is readable. If `Setup()` is skipped or fails silently, subsequent calls throw `FileNotFoundException`. The quick‑checksum algorithm may return an identical string for files that differ only in un‑sampled regions; it is not collision‑resistant.
- **Thread safety**: The class is designed for sequential benchmark execution. `Setup()` and `Cleanup()` mutate shared file state and are not safe to call concurrently. The async checksum methods may be awaited concurrently *after* `Setup()` completes, provided no concurrent `Cleanup()` is in flight, but BenchmarkDotNet runners typically enforce serial iteration semantics.
