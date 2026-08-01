using System;
using BenchmarkDotNet.Attributes;
using DockerSqliteBackup.Exceptions;

namespace DockerSqliteBackup.Benchmarks
{
    [MemoryDiagnoser]
    public class ScheduleExceptionBenchmarks
    {
        // Number of iterations for each benchmark
        [Params(10, 100, 1000)]
        public int Count;

        // Test data prepared once per benchmark run
        private string[] _messages;
        private Guid[] _guids;
        private Exception[] _innerExceptions;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _messages = new string[Count];
            _guids = new Guid[Count];
            _innerExceptions = new Exception[Count];

            for (int i = 0; i < Count; i++)
            {
                _messages[i] = $"Schedule error #{i}";
                _guids[i] = Guid.NewGuid();
                _innerExceptions[i] = new InvalidOperationException($"Inner error {i}");
            }
        }

        /// <summary>
        /// Benchmark creating ScheduleException with string message.
        /// </summary>
        [Benchmark]
        public void ScheduleException_StringConstructor()
        {
            for (int i = 0; i < Count; i++)
            {
                var ex = new ScheduleException(_messages[i]);
                // Use the exception to prevent compiler optimization
                GC.KeepAlive(ex.Message);
            }
        }

        /// <summary>
        /// Benchmark creating ScheduleException with string message and Guid scheduleId.
        /// </summary>
        [Benchmark]
        public void ScheduleException_StringAndGuidConstructor()
        {
            for (int i = 0; i < Count; i++)
            {
                var ex = new ScheduleException(_messages[i], _guids[i]);
                GC.KeepAlive(ex.Message);
                GC.KeepAlive(ex.ScheduleId);
            }
        }

        /// <summary>
        /// Benchmark creating ScheduleException with string message and inner exception.
        /// </summary>
        [Benchmark]
        public void ScheduleException_StringAndExceptionConstructor()
        {
            for (int i = 0; i < Count; i++)
            {
                var ex = new ScheduleException(_messages[i], _innerExceptions[i]);
                GC.KeepAlive(ex.Message);
                GC.KeepAlive(ex.InnerException);
            }
        }
    }
}