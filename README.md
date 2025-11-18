// ... rest of README content ...
## IntegrityCheckerServiceTestsExtensions

The `IntegrityCheckerServiceTestsExtensions` class provides a set of extension methods for testing the `IntegrityCheckerService`. It offers methods for creating test databases with various characteristics and for verifying the service's behavior.

### Usage Example

```csharp
using docker_sqlite_backup.Services;

// Create a test database with complex data
var complexTestDatabase = IntegrityCheckerServiceTestsExtensions.CreateComplexTestDatabase();

// Check that the service indicates the database is healthy
var mockService = IntegrityCheckerServiceTestsExtensions.CreateMockService();
IntegrityCheckerServiceTestsExtensions.ShouldIndicateHealthy(mockService, complexTestDatabase);

// Check that the service indicates corruption in a corrupted database
var corruptedDatabase = IntegrityCheckerServiceTestsExtensions.CreateCorruptedDatabase();
IntegrityCheckerServiceTestsExtensions.ShouldIndicateCorruption(mockService, corruptedDatabase);
```
