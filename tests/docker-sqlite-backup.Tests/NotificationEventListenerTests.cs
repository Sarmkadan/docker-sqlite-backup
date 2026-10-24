using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Events;
using DockerSqliteBackup.Integration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DockerSqliteBackup.Tests
{
    /// <summary>
    /// Tests for the <see cref="NotificationEventListener"/> class, covering constructor and argument validation,
    /// notification fan-out to registered clients, message formatting, and supported event types.
    /// </summary>
    public class NotificationEventListenerTests
    {
        /// <summary>
        /// Creates a new listener with a mocked logger.
        /// </summary>
        private static NotificationEventListener CreateSut() =>
            new(new Mock<ILogger<NotificationEventListener>>().Object);

        /// <summary>
        /// Creates a notification client mock whose <c>SendAsync</c> completes successfully.
        /// </summary>
        private static Mock<INotificationClient> CreateClientMock()
        {
            var client = new Mock<INotificationClient>();
            client.Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
            return client;
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            var act = () => new NotificationEventListener(null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void AddNotificationClient_NullClient_ThrowsArgumentNullException()
        {
            var sut = CreateSut();

            var act = () => sut.AddNotificationClient(null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("client");
        }

        [Fact]
        public async Task HandleAsync_BackupFailedEvent_SendsTitleAndErrorToAddedClient()
        {
            var sut = CreateSut();
            var client = CreateClientMock();
            sut.AddNotificationClient(client.Object);

            await sut.HandleAsync(new BackupFailedEvent { ErrorMessage = "disk full" });

            client.Verify(c => c.SendAsync("✗ Backup Failed", "Backup failed: disk full", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_BackupCompletedEvent_FormatsDurationAndSize()
        {
            var sut = CreateSut();
            var client = CreateClientMock();
            sut.AddNotificationClient(client.Object);

            await sut.HandleAsync(new BackupCompletedEvent
            {
                Duration = TimeSpan.FromSeconds(2.5),
                Result = new BackupResult { BackupFileSizeBytes = 1536 }
            });

            client.Verify(c => c.SendAsync(
                "✓ Backup Completed",
                "Backup completed successfully in 2.50 seconds. Size: 1.5 KB",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(1, "1st")]
        [InlineData(2, "2nd")]
        [InlineData(3, "3rd")]
        [InlineData(4, "4th")]
        public async Task HandleAsync_BackupRetryEvent_UsesOrdinalAttemptNumber(int attempt, string expected)
        {
            var sut = CreateSut();
            var client = CreateClientMock();
            sut.AddNotificationClient(client.Object);

            await sut.HandleAsync(new BackupRetryEvent { AttemptNumber = attempt });

            client.Verify(c => c.SendAsync(
                "⟲ Backup Retry",
                $"Retrying backup (attempt {expected})",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_NullEvent_ThrowsArgumentNullException()
        {
            var sut = CreateSut();

            var act = () => sut.HandleAsync(null!);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task HandleAsync_UnhandledEventType_DoesNotNotifyClients()
        {
            var sut = CreateSut();
            var client = CreateClientMock();
            sut.AddNotificationClient(client.Object);

            await sut.HandleAsync(new BackupStartedEvent());

            client.Verify(c => c.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void GetSupportedEventTypes_ReturnsExpectedTypes_AndCanHandleAcceptsThem()
        {
            var sut = CreateSut();
            var expected = new[]
            {
                "backup.completed",
                "backup.failed",
                "backup.retry",
                "schedule.created",
                "restore.verification.completed",
                "restore.verification.failed"
            };

            sut.GetSupportedEventTypes().Should().BeEquivalentTo(expected);
            foreach (var eventType in expected)
            {
                sut.CanHandle(eventType).Should().BeTrue();
            }
            sut.CanHandle("backup.started").Should().BeFalse();
        }
    }
}
