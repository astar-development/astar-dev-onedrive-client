using AStar.Dev.Logging.Extensions.EventIds;
using NSubstitute;

namespace AStar.Dev.Logging.Extensions;

[TestSubject(typeof(AStarLogger<>))]
public class AStarLoggerTest
{
    private readonly AStarLogger<string> _astLogger;
    private readonly ILogger<string>     _mockLogger;
    private readonly ITelemetryClient    _mockTelemetryClient;

    public AStarLoggerTest()
    {
        _mockLogger = Substitute.For<ILogger<string>>();
        _mockTelemetryClient = Substitute.For<ITelemetryClient>();
        _astLogger = new(_mockLogger, _mockTelemetryClient);
    }

    [Fact]
    public void LogPageView_WhenCalled_TracksPageViewAndLogsInformation()
    {
        const string pageName = "HomePage";

        _astLogger.LogPageView(pageName);

        _mockLogger.Received(1).Log(
                                    LogLevel.Information,
                                    AStarEventIds.Website.PageView,
                                    Arg.Is<object>(obj => obj.ToString() == $"Page view: {pageName}"),
                                    null,
                                    Arg.Any<Func<object, Exception?, string>>());

        _mockTelemetryClient.Received(1).TrackPageView(pageName);
    }

    [Fact]
    public void BeginScope_WhenCalled_ReturnsDisposable()
    {
        var state     = new { Key = "Value" };
        IDisposable mockScope = Substitute.For<IDisposable>();
        _mockLogger.BeginScope(state).Returns(mockScope);

        IDisposable? result = _astLogger.BeginScope(state);

        result.ShouldBeSameAs(mockScope);
        _mockLogger.Received(1).BeginScope(state);
    }

    [Fact]
    public void IsEnabled_WhenLogLevelEnabled_ReturnsTrue()
    {
        const LogLevel logLevel = LogLevel.Debug;
        _mockLogger.IsEnabled(logLevel).Returns(true);

        var result = _astLogger.IsEnabled(logLevel);

        result.ShouldBeTrue();
        _mockLogger.Received(1).IsEnabled(logLevel);
    }

    [Fact]
    public void IsEnabled_WhenLogLevelIsDisabled_ReturnsFalse()
    {
        const LogLevel logLevel = LogLevel.Trace;
        _mockLogger.IsEnabled(logLevel).Returns(false);

        var result = _astLogger.IsEnabled(logLevel);

        result.ShouldBeFalse();
        _mockLogger.Received(1).IsEnabled(logLevel);
    }

    [Fact]
    public void Log_WhenCalled_LogsWithCorrectParameters()
    {
        const LogLevel logLevel  = LogLevel.Warning;
        var            eventId   = new EventId(200, "TestEvent");
        var            state     = new { Message = "This is a warning" };
        var            exception = new Exception("Test exception");

        static string formatter(object s, Exception? e)
        {
            return s.ToString()!;
        }

        _astLogger.Log(logLevel, eventId, state, exception, (Func<object, Exception?, string>)formatter);

        _mockLogger.Received(1).Log(
                                    logLevel,
                                    eventId,
                                    state,
                                    exception,
                                    (Func<object, Exception?, string>)formatter);
    }

    [Fact]
    public void Log_WhenCalledWithNullException_StillLogs()
    {
        const LogLevel logLevel  = LogLevel.Error;
        var            eventId   = new EventId(500, "ErrorEvent");
        var            state     = new { Error = "An error occurred" };
        Exception?     exception = null;

        static string formatter(object s, Exception? e)
        {
            return s.ToString()!;
        }

        _astLogger.Log(logLevel, eventId, state, exception, (Func<object, Exception?, string>)formatter);

        _mockLogger.Received(1).Log(
                                    logLevel,
                                    eventId,
                                    state,
                                    exception,
                                    (Func<object, Exception?, string>)formatter);
    }

    [Fact]
    public void LogPageView_WhenPageNameIsNull_ThrowsArgumentNullException()
    {
        string? pageName = null;

        Should.Throw<ArgumentNullException>(() => _astLogger.LogPageView(pageName!));

        _mockLogger.DidNotReceive().Log(
                                        Arg.Any<LogLevel>(),
                                        Arg.Any<EventId>(),
                                        Arg.Any<object>(),
                                        Arg.Any<Exception?>(),
                                        Arg.Any<Func<object, Exception?, string>>());

        _mockTelemetryClient.DidNotReceive().TrackPageView(Arg.Any<string>());
    }
}
