// ... existing content ...

// ## EncryptionServiceTests
// 
// The `EncryptionServiceTests` class provides a comprehensive set of unit tests for the `EncryptionService` class,
// verifying its behavior across various scenarios including key generation, validation, encryption, and decryption operations.
// These tests ensure the service functions correctly under different configurations and inputs.
//
// ```csharp
// using DockerSqliteBackup.Services;
// using DockerSqliteBackup.Tests.Services;
// using Microsoft.Extensions.Logging;
//
// var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
// var logger = loggerFactory.CreateLogger<EncryptionServiceTests>();
// var tests = new EncryptionServiceTests();
// await tests.InitializeAsync();
// try
// {
// var key = tests.GenerateKey();
// Console.WriteLine($"Generated Key: {key}");
//
// var isValid = tests.ValidateKey(key);
// Console.WriteLine($"Is key valid? {isValid}");
//
// var encryptedFile = await tests.EncryptFileAsync_ThenDecryptFileAsync_RoundTripProducesOriginalContent();
// Console.WriteLine($"Encrypted file: {encryptedFile}");
// }
// finally
// {
// await tests.DisposeAsync();
// }
// ```

// ## MemoryCacheServiceTests

The `MemoryCacheServiceTests` class provides comprehensive unit tests for the `MemoryCacheService` class, verifying its behavior across various caching scenarios including basic operations, expiration, complex types, and asynchronous operations. These tests ensure the service properly handles cache misses, cache hits, expiration policies, and concurrent access patterns.




```csharp
using DockerSqliteBackup.Caching;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<MemoryCacheService>();
var cache = new MemoryCacheService(cleanupInterval: TimeSpan.FromHours(1));

try
{
    // Basic operations
    cache.Set("user-123", new { Name = "John Doe", Email = "john@example.com" });
    var user = cache.Get<object>("user-123");
    Console.WriteLine($"Retrieved user: {user}");
    
    // Check if key exists
    var exists = cache.Exists("user-123");
    Console.WriteLine($"Key exists: {exists}");
    
    // Get or set with factory method
    var value = cache.GetOrSet("config-settings", () => 
    {
        Console.WriteLine("Factory called for cache miss");
        return new { Timeout = 30, Retries = 3 };
    });
    Console.WriteLine($"Value from cache or factory: {value}");
    
    // Set with expiration
    cache.Set("temp-data", "temporary value", TimeSpan.FromSeconds(5));
    Console.WriteLine("Temporary value set with 5 second expiration");
    
    // Asynchronous operations
    await cache.SetAsync("async-counter", 42);
    var asyncValue = await cache.GetAsync<int>("async-counter");
    Console.WriteLine($"Async value: {asyncValue}");
    
    // Remove key
    cache.Remove("user-123");
    Console.WriteLine("Removed user-123 from cache");
    
    // Clear all entries
    cache.Clear();
    Console.WriteLine("Cleared entire cache");
}
finally
{
    // Cleanup
}
```

// ## ScheduleServiceTests
// 
// The `ScheduleServiceTests` class provides unit tests for the `ScheduleService` class, focusing on schedule validation,
// cron expression parsing, and schedule management operations. These tests verify that schedules are properly validated,
// created, retrieved, and deactivated according to business rules.
//
// ```csharp
// using DockerSqliteBackup.Data;
// using DockerSqliteBackup.Domain;
// using DockerSqliteBackup.Services;
// using Microsoft.Extensions.Logging;
// using Moq;
//
// var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
// var logger = loggerFactory.CreateLogger<ScheduleService>();
// var repositoryMock = new Mock<IBackupRepository>();
// var scheduleService = new ScheduleService(repositoryMock.Object, logger);
//
// // Test valid cron expression validation
// var isValidCron = scheduleService.ValidateCronExpression("0 2 * * *");
// Console.WriteLine($"Is cron expression valid? {isValidCron}");
//
// // Test getting next execution time for a valid schedule
// var schedule = new BackupSchedule
// {
//     Name = "Nightly Backup",
//     DatabasePath = "/data/app.db",
//     CronExpression = "0 2 * * *"
// };
// var nextExecution = scheduleService.GetNextExecutionTime(schedule);
// Console.WriteLine($"Next execution time: {nextExecution}");
//
// // Test creating a new schedule
// var newSchedule = new BackupSchedule
// {
//     Name = "Weekly Backup",
//     DatabasePath = "/data/app.db",
//     CronExpression = "0 3 * * 0"
// };
// var createdSchedule = await scheduleService.CreateScheduleAsync(newSchedule);
// Console.WriteLine($"Created schedule: {createdSchedule.Name}");
//
// // Test getting a schedule by ID
// var retrievedSchedule = await scheduleService.GetScheduleAsync(createdSchedule.Id);
// Console.WriteLine($"Retrieved schedule: {retrievedSchedule?.Name}");
//
// // Test deactivating a schedule
// await scheduleService.DeactivateScheduleAsync(createdSchedule.Id);
// var updatedSchedule = await scheduleService.GetScheduleAsync(createdSchedule.Id);
// Console.WriteLine($"Schedule is active: {updatedSchedule?.IsActive}");
// ```

// ## StringUtilityTests

The `StringUtilityTests` class provides comprehensive unit tests for the `StringUtility` class, verifying its behavior across various string manipulation scenarios including formatting, case conversion, truncation, masking, validation, and splitting operations. These tests ensure the utility methods function correctly with different inputs and edge cases.

```csharp
using DockerSqliteBackup.Utilities;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<StringUtilityTests>();

try
{
    // Test FormatBytes with different byte sizes
    var formattedBytes = StringUtility.FormatBytes(1024 * 1024 * 5);
    Console.WriteLine($"Formatted bytes: {formattedBytes}"); // Output: "5 MB"

    // Test case conversion methods
    var kebabCase = StringUtility.ToKebabCase("HelloWorld");
    Console.WriteLine($"Kebab case: {kebabCase}"); // Output: "hello-world"

    var snakeCase = StringUtility.ToSnakeCase("MyPropertyName");
    Console.WriteLine($"Snake case: {snakeCase}"); // Output: "my_property_name"

    var pascalCase = StringUtility.ToPascalCase("hello_world");
    Console.WriteLine($"Pascal case: {pascalCase}"); // Output: "HelloWorld"

    var camelCase = StringUtility.ToCamelCase("my-property-name");
    Console.WriteLine($"Camel case: {camelCase}"); // Output: "myPropertyName"

    // Test string truncation
    var truncated = StringUtility.Truncate("Hello World", 8, true);
    Console.WriteLine($"Truncated: {truncated}"); // Output: "Hello..."

    // Test sensitive data masking
    var masked = StringUtility.MaskSensitive("password123", 4);
    Console.WriteLine($"Masked: {masked}"); // Output: "pass*******"

    // Test email validation
    var isValidEmail = StringUtility.IsValidEmail("test@example.com");
    Console.WriteLine($"Is email valid? {isValidEmail}"); // Output: true

    // Test GUID validation
    var isValidGuid = StringUtility.IsValidGuid("550e8400-e29b-41d4-a716-446655440000");
    Console.WriteLine($"Is GUID valid? {isValidGuid}"); // Output: true

    // Test line splitting
    var lines = StringUtility.SplitLines("line1\r\nline2\rline3\nline4");
    Console.WriteLine($"Lines count: {lines.Length}"); // Output: 4

    // Test readable joining
    var joined = StringUtility.JoinReadable("apple", "banana", "cherry");
    Console.WriteLine($"Joined: {joined}"); // Output: "apple, banana, and cherry"

    // Test whitespace removal
    var noWhitespace = StringUtility.RemoveWhitespace("a b c \t d\n e");
    Console.WriteLine($"No whitespace: {noWhitespace}"); // Output: "abcde"

    // Test conditional quoting
    var quoted = StringUtility.QuoteIfNeeded("with space");
    Console.WriteLine($"Quoted: {quoted}"); // Output: "\"with space\""

    // Test string repetition
    var repeated = StringUtility.Repeat("abc", 3);
    Console.WriteLine($"Repeated: {repeated}"); // Output: "abcabcabc"
}
finally
{
    // Cleanup
}
```