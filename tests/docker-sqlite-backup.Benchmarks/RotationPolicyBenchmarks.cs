using BenchmarkDotNet.Attributes;
using DockerSqliteBackup.Domain;

namespace DockerSqliteBackup.Benchmarks;

[MemoryDiagnoser]
public class RotationPolicyBenchmarks
{
    private RotationPolicy _policy = null!;
    private DateTime _backupDate;

    [Params(10, 50, 100)]
    public int BackupCount { get; set; }

    [Params(5, 15, 30)]
    public int MaxAgeDays { get; set; }

    [Params(2, 5, 10)]
    public int MinBackupCount { get; set; }

    [Params(true, false)]
    public bool VerifyBeforeDeletion { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _policy = new RotationPolicy
        {
            MaxBackupCount = 20,
            MaxAgeDays = MaxAgeDays,
            MinimumBackupCount = MinBackupCount,
            VerifyBeforeDeletion = VerifyBeforeDeletion,
            Strategy = (int)Constants.RotationStrategy.Combined
        };
        _backupDate = DateTime.UtcNow.AddDays(-MaxAgeDays - 1);
    }

    [Benchmark]
    public bool ShouldRotate_CombinedStrategy()
    {
        return _policy.ShouldRotate(BackupCount, _backupDate, false);
    }
}