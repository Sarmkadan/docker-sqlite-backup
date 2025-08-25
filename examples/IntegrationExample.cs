using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DockerSqliteBackup.Configuration;

// Example of integrating with ASP.NET Core DI
var builder = Host.CreateApplicationBuilder(args);

// Assuming appSettings is loaded from your configuration
var appSettings = new AppSettings { DatabasePath = "/path/to/db.db" };

// Wire up the services
builder.Services.AddBackupServices(appSettings);

// You can now inject IBackupService, IStorageService, etc. 
// into your controllers or other services.

var host = builder.Build();

// Run the host
await host.RunAsync();
