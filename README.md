## BackupManifestTests

The BackupManifestTests class contains tests for the BackupManifest class.

### Usage Example

```csharp
BackupManifestTests backupManifestTests = new BackupManifestTests();
backupManifestTests.BackupManifest_DefaultConstructor_SetsDefaultValues();
backupManifestTests.ToJson_SerializesAllProperties();
backupManifestTests.FromJson_DeserializesAllProperties();
backupManifestTests.WriteToFile_CreatesManifestFile();
backupManifestTests.ReadFromFile_NonExistentFile_ReturnsNull();
backupManifestTests.ToJson_HandlesNullValues();
backupManifestTests.ManifestFileNaming_MatchesBackupFile();
```
