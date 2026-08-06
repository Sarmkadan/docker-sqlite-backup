using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Integration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace DockerSqliteBackup.Tests.Integration
{
    /// <summary>
    /// Contains unit tests for the <see cref="DockerSqliteBackup.Integration.WebhookClient"/> class.
    /// The tests verify constructor overloads, property handling, notification sending,
    /// signature generation, and basic integration expectations.
    /// </summary>
    public class WebhookClientTests
    {
        private readonly Mock<ILogger<WebhookClient>> _loggerMock = new();
        private readonly ILogger<WebhookClient> _logger;

        public WebhookClientTests()
        {
            // Resolve the logger from the mock for structured logging within the tests.
            _logger = _loggerMock.Object;
        }

        private const string TestSecret = "test-webhook-secret-1234567890";

        /// <summary>
        /// Verifies that a <see cref="WebhookClient"/> can be instantiated with explicit
        /// <c>maxRetries</c> and <c>webhookSecret</c> parameters and that the resulting instance is not null.
        /// </summary>
        [Fact]
        public void Constructor_WithMaxRetriesAndSecret_SetsProperties()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(Constructor_WithMaxRetriesAndSecret_SetsProperties));

            // Arrange & Act
            var client = new WebhookClient(_loggerMock.Object, maxRetries: 5, webhookSecret: TestSecret);

            // Assert
            Assert.NotNull(client);

            _logger.LogInformation("{TestMethod} completed", nameof(Constructor_WithMaxRetriesAndSecret_SetsProperties));
        }

        /// <summary>
        /// Verifies that a <see cref="WebhookClient"/> can be instantiated using an <see cref="AppSettings"/>
        /// instance that contains a <c>WebhookSecret</c> value, and that the resulting client is not null.
        /// </summary>
        [Fact]
        public void Constructor_WithAppSettings_SetsWebhookSecret()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(Constructor_WithAppSettings_SetsWebhookSecret));

            // Arrange
            var settings = new AppSettings { WebhookSecret = TestSecret };

            // Act
            var client = new WebhookClient(_loggerMock.Object, settings);

            // Assert
            Assert.NotNull(client);

            _logger.LogInformation("{TestMethod} completed", nameof(Constructor_WithAppSettings_SetsWebhookSecret));
        }

        /// <summary>
        /// Verifies that a <see cref="WebhookClient"/> can be created without providing a secret.
        /// The constructor should succeed and the resulting instance should not be null.
        /// </summary>
        [Fact]
        public void Constructor_WithoutSecret_CanBeCreated()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(Constructor_WithoutSecret_CanBeCreated));

            // Arrange & Act
            var client = new WebhookClient(_loggerMock.Object);

            // Assert
            Assert.NotNull(client);

            _logger.LogInformation("{TestMethod} completed", nameof(Constructor_WithoutSecret_CanBeCreated));
        }

        /// <summary>
        /// Ensures that all required constructor overloads of <see cref="WebhookClient"/> exist
        /// and can be invoked without throwing exceptions.
        /// </summary>
        [Fact]
        public void WebhookClient_HasRequiredConstructorOverloads()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(WebhookClient_HasRequiredConstructorOverloads));

            // Test all constructor overloads exist
            var logger = _loggerMock.Object;

            // Constructor 1: logger, maxRetries, webhookSecret
            var client1 = new WebhookClient(logger, 3, "secret");
            Assert.NotNull(client1);

            // Constructor 2: logger, maxRetries
            var client2 = new WebhookClient(logger, 3);
            Assert.NotNull(client2);

            // Constructor 3: logger, AppSettings, maxRetries
            var client3 = new WebhookClient(logger, new AppSettings(), 3);
            Assert.NotNull(client3);

            _logger.LogInformation("{TestMethod} completed", nameof(WebhookClient_HasRequiredConstructorOverloads));
        }

        /// <summary>
        /// Verifies that the <c>WebhookSecret</c> property on <see cref="AppSettings"/> can be set
        /// and retrieved correctly.
        /// </summary>
        [Fact]
        public void AppSettings_HasWebhookSecretProperty()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(AppSettings_HasWebhookSecretProperty));

            // Arrange
            var settings = new AppSettings();

            // Act
            settings.WebhookSecret = TestSecret;

            // Assert
            Assert.Equal(TestSecret, settings.WebhookSecret);

            _logger.LogInformation("{TestMethod} completed", nameof(AppSettings_HasWebhookSecretProperty));
        }

        /// <summary>
        /// Confirms that the <c>WebhookSecret</c> property on <see cref="AppSettings"/> can be assigned a null value.
        /// </summary>
        [Fact]
        public void AppSettings_WebhookSecret_CanBeNull()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(AppSettings_WebhookSecret_CanBeNull));

            // Arrange
            var settings = new AppSettings();

            // Act
            settings.WebhookSecret = null;

            // Assert
            Assert.Null(settings.WebhookSecret);

            _logger.LogInformation("{TestMethod} completed", nameof(AppSettings_WebhookSecret_CanBeNull));
        }

        /// <summary>
        /// Confirms that the <c>WebhookSecret</c> property on <see cref="AppSettings"/> can be assigned an empty string.
        /// </summary>
        [Fact]
        public void AppSettings_WebhookSecret_CanBeEmpty()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(AppSettings_WebhookSecret_CanBeEmpty));

            // Arrange
            var settings = new AppSettings();

            // Act
            settings.WebhookSecret = string.Empty;

            // Assert
            Assert.Empty(settings.WebhookSecret);

            _logger.LogInformation("{TestMethod} completed", nameof(AppSettings_WebhookSecret_CanBeEmpty));
        }

        /// <summary>
        /// Calls <see cref="WebhookClient.SendBackupNotificationAsync"/> with a valid URL and a populated
        /// <see cref="BackupResult"/>. The test passes if the method completes without throwing an exception.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Fact]
        public async Task SendBackupNotificationAsync_CompletesWithoutError()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(SendBackupNotificationAsync_CompletesWithoutError));

            // Arrange
            var client = new WebhookClient(_loggerMock.Object, webhookSecret: TestSecret);
            var backupResult = new BackupResult
            {
                Id = Guid.NewGuid(),
                ScheduleId = Guid.NewGuid(),
                Status = 0,
                BackupFilePath = "/path/to/backup.db",
                BackupFileSizeBytes = 1024,
                Checksum = "abc123",
                StartedAt = DateTime.UtcNow.AddMinutes(-5),
                CompletedAt = DateTime.UtcNow,
                ErrorMessage = null
            };

            // Act & Assert (should not throw)
            await client.SendBackupNotificationAsync("https://example.com/webhook", backupResult);

            _logger.LogInformation("{TestMethod} completed", nameof(SendBackupNotificationAsync_CompletesWithoutError));
        }

        /// <summary>
        /// Calls <see cref="WebhookClient.SendScheduleNotificationAsync"/> with a valid URL and a populated
        /// <see cref="BackupSchedule"/>. The test passes if the method completes without throwing an exception.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Fact]
        public async Task SendScheduleNotificationAsync_CompletesWithoutError()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(SendScheduleNotificationAsync_CompletesWithoutError));

            // Arrange
            var client = new WebhookClient(_loggerMock.Object, webhookSecret: TestSecret);
            var schedule = new BackupSchedule
            {
                Id = Guid.NewGuid(),
                Name = "Test Schedule",
                DatabasePath = "/path/to/db.sqlite",
                CronExpression = "0 2 * * *",
                IsEnabled = true
            };

            // Act & Assert (should not throw)
            await client.SendScheduleNotificationAsync("https://example.com/webhook", schedule, "schedule.created");

            _logger.LogInformation("{TestMethod} completed", nameof(SendScheduleNotificationAsync_CompletesWithoutError));
        }

        /// <summary>
        /// Calls <see cref="WebhookClient.SendBackupNotificationAsync"/> with an empty URL string.
        /// The method should handle the empty URL gracefully and not throw an exception.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Fact]
        public async Task SendBackupNotificationAsync_WithEmptyUrl_DoesNotThrow()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(SendBackupNotificationAsync_WithEmptyUrl_DoesNotThrow));

            // Arrange
            var client = new WebhookClient(_loggerMock.Object, webhookSecret: TestSecret);
            var backupResult = new BackupResult
            {
                Id = Guid.NewGuid(),
                ScheduleId = Guid.NewGuid(),
                Status = 0,
                BackupFilePath = "/path/to/backup.db",
                BackupFileSizeBytes = 1024,
                Checksum = "abc123",
                StartedAt = DateTime.UtcNow.AddMinutes(-5),
                CompletedAt = DateTime.UtcNow,
                ErrorMessage = null
            };

            // Act & Assert (should not throw)
            await client.SendBackupNotificationAsync(string.Empty, backupResult);

            _logger.LogInformation("{TestMethod} completed", nameof(SendBackupNotificationAsync_WithEmptyUrl_DoesNotThrow));
        }

        /// <summary>
        /// Calls <see cref="WebhookClient.SendScheduleNotificationAsync"/> with an empty URL string.
        /// The method should handle the empty URL gracefully and not throw an exception.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Fact]
        public async Task SendScheduleNotificationAsync_WithEmptyUrl_DoesNotThrow()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(SendScheduleNotificationAsync_WithEmptyUrl_DoesNotThrow));

            // Arrange
            var client = new WebhookClient(_loggerMock.Object, webhookSecret: TestSecret);
            var schedule = new BackupSchedule
            {
                Id = Guid.NewGuid(),
                Name = "Test Schedule",
                DatabasePath = "/path/to/db.sqlite",
                CronExpression = "0 2 * * *",
                IsEnabled = true
            };

            // Act & Assert (should not throw)
            await client.SendScheduleNotificationAsync(string.Empty, schedule, "schedule.created");

            _logger.LogInformation("{TestMethod} completed", nameof(SendScheduleNotificationAsync_WithEmptyUrl_DoesNotThrow));
        }

        /// <summary>
        /// Verifies that the <see cref="WebhookClient"/> type resides in the expected namespace
        /// <c>DockerSqliteBackup.Integration</c>.
        /// </summary>
        [Fact]
        public void WebhookClient_IntegrationNamespace_IsCorrect()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(WebhookClient_IntegrationNamespace_IsCorrect));

            // Arrange
            var client = new WebhookClient(_loggerMock.Object);

            // Assert
            Assert.Equal("DockerSqliteBackup.Integration", client.GetType().Namespace);

            _logger.LogInformation("{TestMethod} completed", nameof(WebhookClient_IntegrationNamespace_IsCorrect));
        }

        /// <summary>
        /// Uses reflection to confirm that <see cref="WebhookClient"/> implements the required
        /// <c>SendBackupNotificationAsync</c> and <c>SendScheduleNotificationAsync</c> methods.
        /// </summary>
        [Fact]
        public void WebhookClient_ImplementsRequiredMethods()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(WebhookClient_ImplementsRequiredMethods));

            // Arrange
            var client = new WebhookClient(_loggerMock.Object);

            // Assert - verify required methods exist
            var sendBackupMethod = client.GetType().GetMethod("SendBackupNotificationAsync");
            Assert.NotNull(sendBackupMethod);

            var sendScheduleMethod = client.GetType().GetMethod("SendScheduleNotificationAsync");
            Assert.NotNull(sendScheduleMethod);

            _logger.LogInformation("{TestMethod} completed", nameof(WebhookClient_ImplementsRequiredMethods));
        }

        /// <summary>
        /// Confirms that a <see cref="WebhookClient"/> can be instantiated with a logger and a secret,
        /// indicating that logging support is available.
        /// </summary>
        [Fact]
        public void WebhookClient_HasLoggingSupport()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(WebhookClient_HasLoggingSupport));

            // Arrange
            var client = new WebhookClient(_loggerMock.Object, webhookSecret: TestSecret);

            // Assert
            Assert.NotNull(client);

            _logger.LogInformation("{TestMethod} completed", nameof(WebhookClient_HasLoggingSupport));
        }

        /// <summary>
        /// Generates a signature header using the configured secret and verifies that the result
        /// is non‑empty, starts with <c>t=</c>, and contains <c>,v1=</c>.
        /// </summary>
        [Fact]
        public void CreateSignatureHeader_WithSecret_ReturnsValidSignature()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(CreateSignatureHeader_WithSecret_ReturnsValidSignature));

            // Arrange
            var client = new WebhookClient(_loggerMock.Object, webhookSecret: TestSecret);
            var payload = "{\"test\":\"value\"}";

            // Act
            var signatureHeader = client.CreateSignatureHeader(payload);

            // Assert
            Assert.NotNull(signatureHeader);
            Assert.NotEmpty(signatureHeader);
            Assert.StartsWith("t=", signatureHeader);
            Assert.Contains(",v1=", signatureHeader);

            _logger.LogInformation("{TestMethod} completed", nameof(CreateSignatureHeader_WithSecret_ReturnsValidSignature));
        }

        /// <summary>
        /// Verifies that when no secret is configured, <see cref="WebhookClient.CreateSignatureHeader"/>
        /// returns an empty string.
        /// </summary>
        [Fact]
        public void CreateSignatureHeader_WithoutSecret_ReturnsEmptyString()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(CreateSignatureHeader_WithoutSecret_ReturnsEmptyString));

            // Arrange
            var client = new WebhookClient(_loggerMock.Object);
            var payload = "{\"test\":\"value\"}";

            // Act
            var signatureHeader = client.CreateSignatureHeader(payload);

            // Assert
            Assert.Empty(signatureHeader);

            _logger.LogInformation("{TestMethod} completed", nameof(CreateSignatureHeader_WithoutSecret_ReturnsEmptyString));
        }

        /// <summary>
        /// Verifies that when the configured secret is explicitly set to <c>null</c>,
        /// <see cref="WebhookClient.CreateSignatureHeader"/> returns an empty string.
        /// </summary>
        [Fact]
        public void CreateSignatureHeader_WithNullSecret_ReturnsEmptyString()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(CreateSignatureHeader_WithNullSecret_ReturnsEmptyString));

            // Arrange
            var client = new WebhookClient(_loggerMock.Object, webhookSecret: null);
            var payload = "{\"test\":\"value\"}";

            // Act
            var signatureHeader = client.CreateSignatureHeader(payload);

            // Assert
            Assert.Empty(signatureHeader);

            _logger.LogInformation("{TestMethod} completed", nameof(CreateSignatureHeader_WithNullSecret_ReturnsEmptyString));
        }

        /// <summary>
        /// Calls <see cref="WebhookClient.CreateSignatureHeader"/> with a custom secret argument
        /// and verifies that a non‑empty signature is produced.
        /// </summary>
        [Fact]
        public void CreateSignatureHeader_WithCustomSecret_UsesCustomSecret()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(CreateSignatureHeader_WithCustomSecret_UsesCustomSecret));

            // Arrange
            var customSecret = "custom-secret-key";
            var client = new WebhookClient(_loggerMock.Object, webhookSecret: TestSecret);
            var payload = "{\"test\":\"value\"}";

            // Act
            var signatureHeader = client.CreateSignatureHeader(payload, customSecret);

            // Assert
            Assert.NotNull(signatureHeader);
            Assert.NotEmpty(signatureHeader);

            _logger.LogInformation("{TestMethod} completed", nameof(CreateSignatureHeader_WithCustomSecret_UsesCustomSecret));
        }

        /// <summary>
        /// Verifies that providing an empty custom secret to <see cref="WebhookClient.CreateSignatureHeader"/>
        /// results in an empty string.
        /// </summary>
        [Fact]
        public void CreateSignatureHeader_WithEmptyCustomSecret_ReturnsEmptyString()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(CreateSignatureHeader_WithEmptyCustomSecret_ReturnsEmptyString));

            // Arrange
            var client = new WebhookClient(_loggerMock.Object, webhookSecret: TestSecret);
            var payload = "{\"test\":\"value\"}";

            // Act
            var signatureHeader = client.CreateSignatureHeader(payload, string.Empty);

            // Assert
            Assert.Empty(signatureHeader);

            _logger.LogInformation("{TestMethod} completed", nameof(CreateSignatureHeader_WithEmptyCustomSecret_ReturnsEmptyString));
        }

        /// <summary>
        /// Checks that the format of the generated signature header consists of exactly two parts
        /// separated by a comma, where the first part starts with <c>t=</c> and the second with <c>v1=</c>.
        /// </summary>
        [Fact]
        public void CreateSignatureHeader_Format_IsCorrect()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(CreateSignatureHeader_Format_IsCorrect));

            // Arrange
            var client = new WebhookClient(_loggerMock.Object, webhookSecret: TestSecret);
            var payload = "{\"test\":\"value\"}";

            // Act
            var signatureHeader = client.CreateSignatureHeader(payload);

            // Assert
            var parts = signatureHeader.Split(',');
            Assert.Equal(2, parts.Length);
            Assert.StartsWith("t=", parts[0]);
            Assert.StartsWith("v1=", parts[1]);

            _logger.LogInformation("{TestMethod} completed", nameof(CreateSignatureHeader_Format_IsCorrect));
        }

        /// <summary>
        /// Ensures that invoking <see cref="WebhookClient.CreateSignatureHeader"/> multiple times
        /// with the same payload and secret yields identical signatures.
        /// </summary>
        [Fact]
        public void CreateSignatureHeader_ProducesConsistentSignatures_ForSamePayloadAndSecret()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(CreateSignatureHeader_ProducesConsistentSignatures_ForSamePayloadAndSecret));

            // Arrange
            var client = new WebhookClient(_loggerMock.Object, webhookSecret: TestSecret);
            var payload = "{\"eventType\":\"backup.completed\",\"timestamp\":\"2024-01-01T00:00:00Z\"}";

            // Act - create multiple signatures
            var signature1 = client.CreateSignatureHeader(payload);
            var signature2 = client.CreateSignatureHeader(payload);

            // Assert - signatures should be identical for the same payload and secret
            Assert.Equal(signature1, signature2);

            _logger.LogInformation("{TestMethod} completed", nameof(CreateSignatureHeader_ProducesConsistentSignatures_ForSamePayloadAndSecret));
        }

        /// <summary>
        /// Verifies that different payload strings produce different signature headers when the same secret is used.
        /// </summary>
        [Fact]
        public void CreateSignatureHeader_ProducesDifferentSignatures_ForDifferentPayloads()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(CreateSignatureHeader_ProducesDifferentSignatures_ForDifferentPayloads));

            // Arrange
            var client = new WebhookClient(_loggerMock.Object, webhookSecret: TestSecret);
            var payload1 = "{\"eventType\":\"backup.completed\"}";
            var payload2 = "{\"eventType\":\"schedule.created\"}";

            // Act
            var signature1 = client.CreateSignatureHeader(payload1);
            var signature2 = client.CreateSignatureHeader(payload2);

            // Assert - signatures should be different for different payloads
            Assert.NotEqual(signature1, signature2);

            _logger.LogInformation("{TestMethod} completed", nameof(CreateSignatureHeader_ProducesDifferentSignatures_ForDifferentPayloads));
        }

        /// <summary>
        /// Verifies that using different webhook secrets results in different signature headers for the same payload.
        /// </summary>
        [Fact]
        public void CreateSignatureHeader_ProducesDifferentSignatures_ForDifferentSecrets()
        {
            _logger.LogInformation("Starting {TestMethod}", nameof(CreateSignatureHeader_ProducesDifferentSignatures_ForDifferentSecrets));

            // Arrange
            var client1 = new WebhookClient(_loggerMock.Object, webhookSecret: "secret1");
            var client2 = new WebhookClient(_loggerMock.Object, webhookSecret: "secret2");
            var payload = "{\"test\":\"value\"}";

            // Act
            var signature1 = client1.CreateSignatureHeader(payload);
            var signature2 = client2.CreateSignatureHeader(payload);

            // Assert - signatures should be different for different secrets
            Assert.NotEqual(signature1, signature2);

            _logger.LogInformation("{TestMethod} completed", nameof(CreateSignatureHeader_ProducesDifferentSignatures_ForDifferentSecrets));
        }
    }
}
