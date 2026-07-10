using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DockerSqliteBackup.Tests.Events;

/// <summary>
/// Tests for the <see cref="BackupEventPublisher"/> class, ensuring that event publishing
/// correctly handles listener registration, event dispatch, and exception isolation.
/// </summary>
public class BackupEventPublisherTests
{
    /// <summary>
    /// Mock logger used to verify that the publisher logs appropriately without
    /// requiring a real logger implementation.
    /// </summary>
    private readonly Mock<ILogger<BackupEventPublisher>> _loggerMock;

    /// <summary>
    /// System under test: an instance of <see cref="BackupEventPublisher"/> configured
    /// with the mock logger.
    /// </summary>
    private readonly BackupEventPublisher _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupEventPublisherTests"/> class,
    /// creating the mock logger and the publisher under test.
    /// </summary>
    public BackupEventPublisherTests()
    {
        _loggerMock = new Mock<ILogger<BackupEventPublisher>>();
        _sut = new BackupEventPublisher(_loggerMock.Object);
    }

    /// <summary>
    /// Verifies that publishing an event with no registered listeners does not throw.
    /// </summary>
    [Fact]
    public async Task PublishAsync_NoListeners_DoesNotThrow()
    {
        var @event = new BackupStartedEvent { Schedule = new BackupSchedule { Name = "Test" } };

        var act = async () => await _sut.PublishAsync(@event);

        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies that a listener that can handle the event type is invoked once.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithMatchingListener_InvokesListener()
    {
        var listenerMock = new Mock<IBackupEventListener>();
        listenerMock.Setup(l => l.CanHandle("backup.started")).Returns(true);
        listenerMock.Setup(l => l.GetSupportedEventTypes()).Returns(["backup.started"]);

        _sut.Subscribe(listenerMock.Object);

        var @event = new BackupStartedEvent { Schedule = new BackupSchedule { Name = "Test" } };
        await _sut.PublishAsync(@event);

        listenerMock.Verify(l => l.HandleAsync(@event, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that a listener that cannot handle the event type is not invoked.
    /// </summary>
    [Fact]
    public async Task PublishAsync_ListenerDoesNotMatchEventType_ListenerNotInvoked()
    {
        var listenerMock = new Mock<IBackupEventListener>();
        listenerMock.Setup(l => l.CanHandle(It.IsAny<string>())).Returns(false);
        listenerMock.Setup(l => l.GetSupportedEventTypes()).Returns(["backup.completed"]);

        _sut.Subscribe(listenerMock.Object);

        var @event = new BackupStartedEvent { Schedule = new BackupSchedule { Name = "Test" } };
        await _sut.PublishAsync(@event);

        listenerMock.Verify(l => l.HandleAsync(It.IsAny<BackupEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies that an exception thrown by a listener does not propagate to the caller.
    /// </summary>
    [Fact]
    public async Task PublishAsync_ListenerThrows_DoesNotPropagateException()
    {
        var listenerMock = new Mock<IBackupEventListener>();
        listenerMock.Setup(l => l.CanHandle("backup.started")).Returns(true);
        listenerMock.Setup(l => l.GetSupportedEventTypes()).Returns(["backup.started"]);
        listenerMock
            .Setup(l => l.HandleAsync(It.IsAny<BackupEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Listener failed"));

        _sut.Subscribe(listenerMock.Object);

        var @event = new BackupStartedEvent { Schedule = new BackupSchedule { Name = "Test" } };
        var act = async () => await _sut.PublishAsync(@event);

        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies that all listeners registered for an event type are invoked.
    /// </summary>
    [Fact]
    public async Task PublishAsync_MultipleMatchingListeners_AllInvoked()
    {
        var listener1 = new Mock<IBackupEventListener>();
        listener1.Setup(l => l.CanHandle("backup.completed")).Returns(true);
        listener1.Setup(l => l.GetSupportedEventTypes()).Returns(["backup.completed"]);

        var listener2 = new Mock<IBackupEventListener>();
        listener2.Setup(l => l.CanHandle("backup.completed")).Returns(true);
        listener2.Setup(l => l.GetSupportedEventTypes()).Returns(["backup.completed"]);

        _sut.Subscribe(listener1.Object);
        _sut.Subscribe(listener2.Object);

        var @event = new BackupCompletedEvent { Result = new BackupResult() };
        await _sut.PublishAsync(@event);

        listener1.Verify(l => l.HandleAsync(@event, It.IsAny<CancellationToken>()), Times.Once);
        listener2.Verify(l => l.HandleAsync(@event, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that subscribing the same listener twice results in a single registration.
    /// </summary>
    [Fact]
    public async Task Subscribe_SameListenerTwice_RegistersOnlyOnce()
    {
        var listenerMock = new Mock<IBackupEventListener>();
        listenerMock.Setup(l => l.GetSupportedEventTypes()).Returns(["backup.started"]);

        _sut.Subscribe(listenerMock.Object);
        _sut.Subscribe(listenerMock.Object);

        // We can't directly inspect the list, but we verify HandleAsync is called only once
        listenerMock.Setup(l => l.CanHandle("backup.started")).Returns(true);
        var @event = new BackupStartedEvent { Schedule = new BackupSchedule() };

        await _sut.PublishAsync(@event);

        listenerMock.Verify(l => l.HandleAsync(It.IsAny<BackupEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that unsubscribing a registered listener removes it from future event dispatches.
    /// </summary>
    [Fact]
    public async Task Unsubscribe_RegisteredListener_NoLongerReceivesEvents()
    {
        var listenerMock = new Mock<IBackupEventListener>();
        listenerMock.Setup(l => l.CanHandle("backup.started")).Returns(true);
        listenerMock.Setup(l => l.GetSupportedEventTypes()).Returns(["backup.started"]);

        _sut.Subscribe(listenerMock.Object);
        _sut.Unsubscribe(listenerMock.Object);

        var @event = new BackupStartedEvent { Schedule = new BackupSchedule() };
        await _sut.PublishAsync(@event);

        listenerMock.Verify(l => l.HandleAsync(It.IsAny<BackupEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies that attempting to unsubscribe a listener that was never registered does not throw.
    /// </summary>
    [Fact]
    public void Unsubscribe_UnregisteredListener_DoesNotThrow()
    {
        var listenerMock = new Mock<IBackupEventListener>();
        listenerMock.Setup(l => l.GetSupportedEventTypes()).Returns([]);

        var act = () => _sut.Unsubscribe(listenerMock.Object);

        act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that if one listener throws an exception, other listeners still receive the event.
    /// </summary>
    [Fact]
    public async Task PublishAsync_OneListenerFails_OtherListenerStillInvoked()
    {
        var failingListener = new Mock<IBackupEventListener>();
        failingListener.Setup(l => l.CanHandle("backup.failed")).Returns(true);
        failingListener.Setup(l => l.GetSupportedEventTypes()).Returns(["backup.failed"]);
        failingListener
            .Setup(l => l.HandleAsync(It.IsAny<BackupEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Listener error"));

        var workingListener = new Mock<IBackupEventListener>();
        workingListener.Setup(l => l.CanHandle("backup.failed")).Returns(true);
        workingListener.Setup(l => l.GetSupportedEventTypes()).Returns(["backup.failed"]);

        _sut.Subscribe(failingListener.Object);
        _sut.Subscribe(workingListener.Object);

        var @event = new BackupFailedEvent { ScheduleId = Guid.NewGuid(), ErrorMessage = "error" };
        await _sut.PublishAsync(@event);

        workingListener.Verify(l => l.HandleAsync(@event, It.IsAny<CancellationToken>()), Times.Once);
    }
}
