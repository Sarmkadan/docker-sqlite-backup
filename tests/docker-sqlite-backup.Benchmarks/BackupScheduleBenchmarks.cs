using BenchmarkDotNet.Attributes;
using DockerSqliteBackup.Domain;

namespace DockerSqliteBackup.Benchmarks;

[MemoryDiagnoser]
public class BackupScheduleBenchmarks
{
    [Params(10, 100, 1000)]
    public int BatchSize;

    private BackupSchedule _schedule = null!;
    private List<BackupSchedule> _schedules = null!;

    [GlobalSetup]
    public void Setup()
    {
        _schedule = new BackupSchedule
        {
            Name = "DailyBackup",
            DatabasePath = "/path/to/db.sqlite",
            CronExpression = "0 2 * * *",
            RetentionDays = 30,
            MaxBackupCount = 10
        };

        _schedules = new List<BackupSchedule>();
        for (int i = 0; i < BatchSize; i++)
        {
            _schedules.Add(new BackupSchedule { Name = $"Schedule_{i}", DatabasePath = "/path/to/db.sqlite" });
        }
    }

    [Benchmark]
    public bool IsValid() => _schedule.IsValid();

    [Benchmark]
    public DateTime? GetNextRunTime() => _schedule.GetNextRunTime();

    [Benchmark]
    public string GetBackupName() => _schedule.GetBackupName();

    [Benchmark]
    public string GetBackupFileExtension() => _schedule.GetBackupFileExtension();

    [Benchmark]
    public int BatchIsValid()
    {
        int validCount = 0;
        foreach (var schedule in _schedules)
        {
            if (schedule.IsValid())
            {
                validCount++;
            }
        }
        return validCount;
    }
}
