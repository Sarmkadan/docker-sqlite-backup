using DockerSqliteBackup.Events;
using DockerSqliteBackup.Integration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DockerSqliteBackup.Tests
{
    /// <summary>
    /// Tests for the <see cref="NotificationEventListenerExtensions"/> class, ensuring that extension methods
    /// for <see cref="NotificationEventListener"/> work correctly.
    /// </summary>
    public class NotificationEventListenerExtensionsTests
    {
        /// <summary>
        /// Mock logger used to create the <see cref="NotificationEventListener"/> instance.
        /// </summary>
        private readonly Mock<ILogger<NotificationEventListener>> _loggerMock;

        /// <summary>
        /// System under test: an instance of <see cref="NotificationEventListener"/>
        /// </summary>
        private readonly NotificationEventListener _sut;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationEventListenerExtensionsTests"/> class.
        /// </summary>
        public NotificationEventListenerExtensionsTests()
        {
            _loggerMock = new Mock<ILogger<NotificationEventListener>>();
            _sut = new NotificationEventListener(_loggerMock.Object);
        }

        #region AddNotificationClients Tests

        /// <summary>
        /// Verifies that AddNotificationClients with IEnumerable works correctly.
        /// </summary>
        [Fact]
        public void AddNotificationClients_WithIEnumerable_AddsAllClients()
        {
            // Arrange
            var client1 = new Mock<INotificationClient>();
            var client2 = new Mock<INotificationClient>();
            var clients = new List<INotificationClient> { client1.Object, client2.Object };

            // Act
            _sut.AddNotificationClients(clients);

            // Assert - verify the clients were added by checking internal state
            var field = typeof(NotificationEventListener).GetField("_notificationClients",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var notificationClients = field?.GetValue(_sut) as List<INotificationClient>;

            notificationClients.Should().NotBeNull();
            notificationClients.Should().HaveCount(3); // 1 default console client + 2 added clients
        }

        /// <summary>
        /// Verifies that AddNotificationClients with IEnumerable throws when listener is null.
        /// </summary>
        [Fact]
        public void AddNotificationClients_WithIEnumerable_NullListener_ThrowsArgumentNullException()
        {
            // Arrange
            var client1 = new Mock<INotificationClient>();
            var clients = new List<INotificationClient> { client1.Object };

            // Act
            Action act = () => ((NotificationEventListener)null!).AddNotificationClients(clients);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Verifies that AddNotificationClients with IEnumerable throws when clients collection is null.
        /// </summary>
        [Fact]
        public void AddNotificationClients_WithIEnumerable_NullClients_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => _sut.AddNotificationClients((IEnumerable<INotificationClient>)null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Verifies that AddNotificationClients with IEnumerable handles empty collection.
        /// </summary>
        [Fact]
        public void AddNotificationClients_WithIEnumerable_EmptyCollection_DoesNotThrow()
        {
            // Arrange
            var clients = new List<INotificationClient>();

            // Act
            Action act = () => _sut.AddNotificationClients(clients);

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// Verifies that AddNotificationClients with factory method works correctly.
        /// </summary>
        [Fact]
        public void AddNotificationClients_WithFactoryMethod_AddsClientsFromFactory()
        {
            // Arrange
            var client1 = new Mock<INotificationClient>();
            var client2 = new Mock<INotificationClient>();
            var clients = new List<INotificationClient> { client1.Object, client2.Object };

            // Act
            _sut.AddNotificationClients(() => clients);

            // Assert
            var field = typeof(NotificationEventListener).GetField("_notificationClients",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var notificationClients = field?.GetValue(_sut) as List<INotificationClient>;

            notificationClients.Should().NotBeNull();
            notificationClients.Should().HaveCount(3); // 1 default console client + 2 added clients
        }

        /// <summary>
        /// Verifies that AddNotificationClients with factory method throws when listener is null.
        /// </summary>
        [Fact]
        public void AddNotificationClients_WithFactoryMethod_NullListener_ThrowsArgumentNullException()
        {
            // Arrange
            var client = new Mock<INotificationClient>();
            var clients = new List<INotificationClient> { client.Object };

            // Act
            Action act = () => ((NotificationEventListener)null!).AddNotificationClients(() => clients);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Verifies that AddNotificationClients with factory method throws when factory is null.
        /// </summary>
        [Fact]
        public void AddNotificationClients_WithFactoryMethod_NullFactory_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => _sut.AddNotificationClients((Func<IEnumerable<INotificationClient>>)null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region HandleAsync Tests

        /// <summary>
        /// Verifies that HandleAsync throws ArgumentNullException when listener is null.
        /// </summary>
        [Fact]
        public async Task HandleAsync_NullListener_ThrowsArgumentNullException()
        {
            // Act
            Func<Task> act = async () => await ((NotificationEventListener)null!).HandleAsync("backup.completed");

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Verifies that HandleAsync throws ArgumentNullException when eventType is null.
        /// </summary>
        [Fact]
        public async Task HandleAsync_NullEventType_ThrowsArgumentNullException()
        {
            // Act
            Func<Task> act = () => _sut.HandleAsync(null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Verifies that HandleAsync throws ArgumentException when eventType is empty.
        /// </summary>
        [Fact]
        public async Task HandleAsync_EmptyEventType_ThrowsArgumentException()
        {
            // Act
            Func<Task> act = async () => await _sut.HandleAsync(string.Empty);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        /// <summary>
        /// Verifies that HandleAsync throws ArgumentException when eventType is whitespace.
        /// </summary>
        [Fact]
        public async Task HandleAsync_WhitespaceEventType_ThrowsArgumentException()
        {
            // Act
            Func<Task> act = async () => await _sut.HandleAsync("   ");

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        /// <summary>
        /// Verifies that HandleAsync throws InvalidOperationException when event type is not supported.
        /// </summary>
        [Fact]
        public async Task HandleAsync_UnsupportedEventType_ThrowsInvalidOperationException()
        {
            // Arrange - create a new listener that can't handle any events
            var loggerMock = new Mock<ILogger<NotificationEventListener>>();
            var listener = new NotificationEventListener(loggerMock.Object);

            // Act
            Func<Task> act = async () => await listener.HandleAsync("unknown.event");

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        #endregion

        #region GetSupportedEventTypesSet Tests

        /// <summary>
        /// Verifies that GetSupportedEventTypesSet returns all supported event types.
        /// </summary>
        [Fact]
        public void GetSupportedEventTypesSet_ReturnsAllSupportedTypes()
        {
            // Act
            var supportedTypes = _sut.GetSupportedEventTypesSet();

            // Assert
            supportedTypes.Should().BeEquivalentTo(new HashSet<string>
            {
                "backup.completed",
                "backup.failed",
                "backup.retry",
                "schedule.created",
                "restore.verification.completed"
            });
        }

        /// <summary>
        /// Verifies that GetSupportedEventTypesSet throws when listener is null.
        /// </summary>
        [Fact]
        public void GetSupportedEventTypesSet_NullListener_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => ((NotificationEventListener)null!).GetSupportedEventTypesSet();

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Verifies that GetSupportedEventTypesSet returns a HashSet.
        /// </summary>
        [Fact]
        public void GetSupportedEventTypesSet_ReturnsHashSet()
        {
            // Act
            var result = _sut.GetSupportedEventTypesSet();

            // Assert
            result.Should().BeOfType<HashSet<string>>();
            result.Should().NotBeNull();
        }

        #endregion

        #region CanHandleAny Tests

        /// <summary>
        /// Verifies that CanHandleAny returns true when listener can handle any of the provided event types.
        /// </summary>
        [Fact]
        public void CanHandleAny_ReturnsTrue_WhenAnyTypeIsSupported()
        {
            // Arrange
            var eventTypes = new List<string> { "backup.completed", "unknown.type" };

            // Act
            var result = _sut.CanHandleAny(eventTypes);

            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that CanHandleAny returns false when listener cannot handle any of the provided event types.
        /// </summary>
        [Fact]
        public void CanHandleAny_ReturnsFalse_WhenNoTypesAreSupported()
        {
            // Arrange
            var eventTypes = new List<string> { "unknown.type1", "unknown.type2" };

            // Act
            var result = _sut.CanHandleAny(eventTypes);

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that CanHandleAny handles empty collection.
        /// </summary>
        [Fact]
        public void CanHandleAny_EmptyCollection_ReturnsFalse()
        {
            // Arrange
            var eventTypes = new List<string>();

            // Act
            var result = _sut.CanHandleAny(eventTypes);

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that CanHandleAny throws when listener is null.
        /// </summary>
        [Fact]
        public void CanHandleAny_NullListener_ThrowsArgumentNullException()
        {
            // Arrange
            var eventTypes = new List<string> { "backup.completed" };

            // Act
            Action act = () => ((NotificationEventListener)null!).CanHandleAny(eventTypes);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Verifies that CanHandleAny throws when eventTypes collection is null.
        /// </summary>
        [Fact]
        public void CanHandleAny_NullEventTypes_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => _sut.CanHandleAny(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion
    }
}
