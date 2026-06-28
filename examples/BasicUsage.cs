using Microsoft.Extensions.DependencyInjection;
using DockerSqliteBackup.Configuration;

// Basic setup of the backup tool services
var services = new ServiceCollection();

// Configure your settings
var appSettings = new AppSettings
{
    DatabasePath = "/path/to/your/database.db",
    // Set other required settings here...
};

// Add the backup services to the dependency injection container
services.AddBackupServices(appSettings);

// Build your service provider
var serviceProvider = services.BuildServiceProvider();

// Initialize the database
await serviceProvider.InitializeDatabaseAsync();

Console.WriteLine("Services configured and database initialized.");
