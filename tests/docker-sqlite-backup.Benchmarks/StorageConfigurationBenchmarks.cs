using BenchmarkDotNet.Attributes;
using DockerSqliteBackup.Domain;

namespace DockerSqliteBackup.Benchmarks;

[MemoryDiagnoser]
public class StorageConfigurationBenchmarks
{
    private List<StorageConfiguration> _configurations = new();

    [Params(10, 100, 1000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        _configurations = new List<StorageConfiguration>();
        for (int i = 0; i < N; i++)
        {
            _configurations.Add(new LocalStorageConfiguration
            {
                Name = $"TestConfig_{i}",
                BaseDirectory = $"/tmp/test_{i}",
                IsDefault = i == 0
            });
        }
    }

    [Benchmark]
    public void DeepCopy()
    {
        foreach (var config in _configurations)
        {
            _ = config.DeepCopy();
        }
    }

    [Benchmark]
    public void IsCloudStorage()
    {
        foreach (var config in _configurations)
        {
            _ = config.IsCloudStorage();
        }
    }

    [Benchmark]
    public void GetDisplayName()
    {
        foreach (var config in _configurations)
        {
            _ = config.GetDisplayName();
        }
    }

    [Benchmark]
    public void ValidateName()
    {
        foreach (var config in _configurations)
        {
            _ = config.ValidateName();
        }
    }

    [Benchmark]
    public void GetAgeInDays()
    {
        foreach (var config in _configurations)
        {
            _ = config.GetAgeInDays();
        }
    }
}
