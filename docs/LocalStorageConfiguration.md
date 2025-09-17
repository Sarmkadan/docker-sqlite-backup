# LocalStorageConfiguration
The `LocalStorageConfiguration` type is designed to manage the configuration settings for storing backups locally. It provides properties to customize the backup storage, such as the base directory, file permissions, and compression settings. Additionally, it includes methods to test the connection and determine the backup path.

## API
* `public string BaseDirectory`: Gets the base directory where backups are stored.
* `public bool CreateSubdirectoriesBySchedule`: Gets a value indicating whether subdirectories are created based on the schedule.
* `public string FilePermissions`: Gets the file permissions for the backup files.
* `public bool CompressBackups`: Gets a value indicating whether backups are compressed.
* `public long MinimumFreeSpaceBytes`: Gets the minimum free space required in bytes.
* `public bool PreserveFileTimestamp`: Gets a value indicating whether the file timestamp is preserved.
* `public override bool IsValid`: Gets a value indicating whether the configuration is valid.
* `public override async Task<bool> TestConnectionAsync`: Tests the connection asynchronously. Returns `true` if the connection is successful, `false` otherwise. May throw exceptions if the connection fails.
* `public string GetBackupPath`: Gets the path where the backup is stored.

## Usage
The following examples demonstrate how to use the `LocalStorageConfiguration` type:
```csharp
// Example 1: Configuring local storage settings
var config = new LocalStorageConfiguration
{
    BaseDirectory = "/backups",
    CreateSubdirectoriesBySchedule = true,
    FilePermissions = "644",
    CompressBackups = true,
    MinimumFreeSpaceBytes = 1024 * 1024 * 1024, // 1 GB
    PreserveFileTimestamp = true
};

if (config.IsValid)
{
    Console.WriteLine("Configuration is valid");
}
else
{
    Console.WriteLine("Configuration is invalid");
}

// Example 2: Testing the connection and getting the backup path
var config2 = new LocalStorageConfiguration
{
    BaseDirectory = "/backups"
};

if (await config2.TestConnectionAsync())
{
    var backupPath = config2.GetBackupPath;
    Console.WriteLine($"Backup path: {backupPath}");
}
else
{
    Console.WriteLine("Connection test failed");
}
```

## Notes
When using the `LocalStorageConfiguration` type, consider the following edge cases and thread-safety remarks:
* The `TestConnectionAsync` method may throw exceptions if the connection fails. It is recommended to handle these exceptions properly.
* The `GetBackupPath` method returns a string representing the backup path. This path may not exist if the `CreateSubdirectoriesBySchedule` property is `false`.
* The `MinimumFreeSpaceBytes` property ensures that there is sufficient free space available before storing backups. If the available free space is less than the specified minimum, the backup process may fail.
* The `LocalStorageConfiguration` type is not thread-safe by default. If multiple threads access the same instance, it is recommended to implement proper synchronization mechanisms to avoid data corruption or other concurrency issues.
