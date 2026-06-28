using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Services;

var services = new ServiceCollection();

// Configure logging
services.AddLogging(configure => configure.AddConsole());

// Advanced settings
var appSettings = new AppSettings
{
    DatabasePath = "/path/to/your/database.db",
    LogLevel = "Debug",
    // Configure encryption, storage, etc.
};

services.AddBackupServices(appSettings);

var serviceProvider = services.BuildServiceProvider();

try
{
    await serviceProvider.InitializeDatabaseAsync();
    
    // Resolve a specific service to perform a task, for example:
    var backupService = serviceProvider.GetRequiredService<IBackupService>();
    
    Console.WriteLine("Performing manual backup...");
    await backupService.ExecuteBackupAsync();
    Console.WriteLine("Backup completed successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred during operation: {ex.Message}");
}
