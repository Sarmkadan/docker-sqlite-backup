using System;
using BenchmarkDotNet.Attributes;
using DockerSqliteBackup.Exceptions;

namespace DockerSqliteBackup.Benchmarks
{
    /// <summary>
    /// Benchmarks for the various <see cref="VerificationException"/> derived types.
    /// </summary>
    [MemoryDiagnoser]
    public class VerificationExceptionBenchmarks
    {
        // Number of iterations for each benchmark
        [Params(10, 100, 1000)]
        public int Count;

        // Test data prepared once per benchmark run
        private string[] _messages;
        private Guid[] _guids;
        private string[] _errorMessages;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _messages = new string[Count];
            _guids = new Guid[Count];
            _errorMessages = new string[Count];

            for (int i = 0; i < Count; i++)
            {
                _messages[i] = $"Verification error #{i}";
                _guids[i] = Guid.NewGuid();
                _errorMessages[i] = $"Error detail {i}: some simulated integrity problem.";
            }
        }

        /// <summary>
        /// Benchmark creating plain <see cref="VerificationException"/> instances.
        /// </summary>
        [Benchmark]
        public void CreateVerificationExceptions()
        {
            for (int i = 0; i < Count; i++)
            {
                var ex = new VerificationException(_messages[i], _guids[i]);
                // Access a property to avoid dead‑code elimination
                var _ = ex.Message;
            }
        }

        /// <summary>
        /// Benchmark creating <see cref="IntegrityCheckFailedException"/> instances.
        /// </summary>
        [Benchmark]
        public void CreateIntegrityCheckFailedExceptions()
        {
            for (int i = 0; i < Count; i++)
            {
                var ex = new IntegrityCheckFailedException(_messages[i], _guids[i], _errorMessages[i]);
                var _ = ex.Errors;
            }
        }

        /// <summary>
        /// Benchmark creating <see cref="RestoreVerificationFailedException"/> instances.
        /// </summary>
        [Benchmark]
        public void CreateRestoreVerificationFailedExceptions()
        {
            for (int i = 0; i < Count; i++)
            {
                var ex = new RestoreVerificationFailedException(_messages[i], _guids[i]);
                var _ = ex.Message;
            }
        }

        /// <summary>
        /// Benchmark throwing and catching a <see cref="VerificationException"/>.
        /// </summary>
        [Benchmark]
        public void ThrowAndCatchVerificationException()
        {
            for (int i = 0; i < Count; i++)
            {
                try
                {
                    throw new VerificationException(_messages[i], _guids[i]);
                }
                catch (VerificationException ex)
                {
                    // Access a property to keep the catch block from being optimized away
                    var _ = ex.Message;
                }
            }
        }
    }
}
