#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DockerSqliteBackup.BulkTransfer;

/// <summary>
/// Extension methods for registering the bulk transfer subsystem and composing
/// <see cref="IAsyncEnumerable{T}"/> chunk streams.
/// </summary>
public static class BulkTransferExtensions
{
    /// <summary>
    /// Registers <see cref="BulkTransferService"/> and its configuration options with
    /// the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">
    /// Optional delegate to override <see cref="BulkTransferOptions"/> defaults.
    /// If <c>null</c>, values are bound exclusively from the
    /// <see cref="BulkTransferOptions.SectionKey"/> configuration section.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddBulkTransfer(opts =>
    /// {
    ///     opts.ChunkSizeBytes = 131_072;   // 128 KiB chunks
    ///     opts.MaxConcurrentExports = 8;
    ///     opts.JobRetentionPeriod = TimeSpan.FromHours(48);
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddBulkTransfer(
        this IServiceCollection services,
        Action<BulkTransferOptions>? configure = null)
    {
        var optionsBuilder = services.AddOptions<BulkTransferOptions>()
            .BindConfiguration(BulkTransferOptions.SectionKey)
            .ValidateOnStart();

        if (configure is not null)
            optionsBuilder.PostConfigure(configure);

        services.TryAddScoped<IBulkTransferService, BulkTransferService>();

        return services;
    }

    /// <summary>
    /// Collects an entire <see cref="IAsyncEnumerable{BulkTransferChunk}"/> export stream
    /// into a single <see cref="byte"/> array.
    /// </summary>
    /// <remarks>
    /// Intended for small exports and unit-testing scenarios.
    /// For large archives use <see cref="CopyToAsync"/> to avoid materialising the
    /// full archive in heap memory.
    /// </remarks>
    /// <param name="chunks">The async chunk stream returned by <see cref="IBulkTransferService.ExportAsync"/>.</param>
    /// <param name="cancellationToken">Cancels the enumeration.</param>
    /// <returns>A byte array containing the complete archive.</returns>
    public static async Task<byte[]> ToByteArrayAsync(
        this IAsyncEnumerable<BulkTransferChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        await foreach (var chunk in chunks.WithCancellation(cancellationToken))
            ms.Write(chunk.Data.Span);
        return ms.ToArray();
    }

    /// <summary>
    /// Pipes an export chunk stream directly into an arbitrary <see cref="Stream"/>,
    /// avoiding intermediate heap allocation.
    /// </summary>
    /// <param name="chunks">The async chunk stream.</param>
    /// <param name="destination">The writable target stream (e.g. an HTTP response body).</param>
    /// <param name="cancellationToken">Cancels the copy operation.</param>
    public static async Task CopyToAsync(
        this IAsyncEnumerable<BulkTransferChunk> chunks,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in chunks.WithCancellation(cancellationToken))
            await destination.WriteAsync(chunk.Data, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Counts the total bytes across all chunks in the stream without buffering.
    /// Consumes the entire enumerable.
    /// </summary>
    /// <param name="chunks">The async chunk stream.</param>
    /// <param name="cancellationToken">Cancels the enumeration.</param>
    /// <returns>Total byte count of the streamed archive.</returns>
    public static async Task<long> CountBytesAsync(
        this IAsyncEnumerable<BulkTransferChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        var total = 0L;
        await foreach (var chunk in chunks.WithCancellation(cancellationToken))
            total += chunk.Data.Length;
        return total;
    }

    /// <summary>
    /// Filters the chunk stream to yield only the data bytes, stripping all metadata fields.
    /// Useful when composing the stream with other pipeline stages that operate on raw bytes.
    /// </summary>
    /// <param name="chunks">The async chunk stream.</param>
    /// <param name="cancellationToken">Cancels the enumeration.</param>
    /// <returns>An async sequence of <see cref="ReadOnlyMemory{Byte}"/> values.</returns>
    public static async IAsyncEnumerable<ReadOnlyMemory<byte>> SelectDataAsync(
        this IAsyncEnumerable<BulkTransferChunk> chunks,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in chunks.WithCancellation(cancellationToken))
            yield return chunk.Data;
    }

    /// <summary>
    /// Wraps an export chunk stream to invoke a progress callback after every chunk,
    /// reporting the running byte total.
    /// </summary>
    /// <param name="chunks">The async chunk stream.</param>
    /// <param name="onChunk">
    /// Callback receiving (<em>bytesDeliveredSoFar</em>, <em>chunkSequenceNumber</em>) after each yield.
    /// </param>
    /// <param name="cancellationToken">Cancels the enumeration.</param>
    /// <returns>The original chunk stream with side-effect progress reporting.</returns>
    public static async IAsyncEnumerable<BulkTransferChunk> WithChunkProgress(
        this IAsyncEnumerable<BulkTransferChunk> chunks,
        Action<long, long> onChunk,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var bytesTotal = 0L;
        await foreach (var chunk in chunks.WithCancellation(cancellationToken))
        {
            bytesTotal += chunk.Data.Length;
            onChunk(bytesTotal, chunk.SequenceNumber);
            yield return chunk;
        }
    }

    /// <summary>
    /// Converts a <see cref="TransferJobState"/> value to a human-readable label.
    /// </summary>
    public static string ToDisplayString(this TransferJobState state) => state switch
    {
        TransferJobState.Queued    => "Queued",
        TransferJobState.Running   => "Running",
        TransferJobState.Completed => "Completed",
        TransferJobState.Failed    => "Failed",
        TransferJobState.Cancelled => "Cancelled",
        _                          => state.ToString()
    };

    /// <summary>
    /// Returns <c>true</c> when the job is in a terminal state and will not produce
    /// further progress updates.
    /// </summary>
    public static bool IsTerminal(this TransferJobState state) =>
        state is TransferJobState.Completed or TransferJobState.Failed or TransferJobState.Cancelled;

    /// <summary>
    /// Returns <c>true</c> when the job has been registered but the transfer has not yet started.
    /// </summary>
    public static bool IsPending(this TransferJobState state) =>
        state is TransferJobState.Queued or TransferJobState.Running;
}
