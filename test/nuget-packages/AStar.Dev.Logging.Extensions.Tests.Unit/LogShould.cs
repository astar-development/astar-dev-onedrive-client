using NSubstitute;

namespace AStar.Dev.Logging.Extensions.Tests.Unit;

public class LogShould
{
    [Theory]
    [InlineData("/api/resource")]
    public void BadRequest_ShouldLog(string path)
    {
        ILogger logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        Messages.AStarLog.Web.BadRequest(logger, path);

        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Is<EventId>(e => e.Id == 400),
            Arg.Any<object?>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object?, Exception?, string>>()
        );
    }

    [Theory]
    [InlineData("/api/resource")]
    public void Unauthorized_ShouldLog(string path)
    {
        ILogger logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        Messages.AStarLog.Web.Unauthorized(logger, path);

        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Is<EventId>(e => e.Id == 401),
            Arg.Any<object?>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object?, Exception?, string>>()
        );
    }

    [Theory]
    [InlineData("/api/resource")]
    public void Forbidden_ShouldLog(string path)
    {
        ILogger logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        Messages.AStarLog.Web.Forbidden(logger, path);

        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Is<EventId>(e => e.Id == 403),
            Arg.Any<object?>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object?, Exception?, string>>()
        );
    }

    [Theory]
    [InlineData("/api/resource")]
    public void NotFound_ShouldLog(string path)
    {
        ILogger logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        Messages.AStarLog.Web.NotFound(logger, path);

        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Is<EventId>(e => e.Id == 404),
            Arg.Any<object?>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object?, Exception?, string>>()
        );
    }

    [Theory]
    [InlineData("/api/resource")]
    public void Conflict_ShouldLog(string path)
    {
        ILogger logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        Messages.AStarLog.Web.Conflict(logger, path);

        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Is<EventId>(e => e.Id == 409),
            Arg.Any<object?>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object?, Exception?, string>>()
        );
    }

    [Theory]
    [InlineData("/api/resource")]
    public void UnprocessableEntity_ShouldLog(string path)
    {
        ILogger logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        Messages.AStarLog.Web.UnprocessableEntity(logger, path);

        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Is<EventId>(e => e.Id == 422),
            Arg.Any<object?>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object?, Exception?, string>>()
        );
    }

    [Theory]
    [InlineData("/api/resource")]
    public void TooManyRequests_ShouldLog(string path)
    {
        ILogger logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        Messages.AStarLog.Web.TooManyRequests(logger, path);

        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Is<EventId>(e => e.Id == 429),
            Arg.Any<object?>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object?, Exception?, string>>()
        );
    }

    [Theory]
    [InlineData("/api/resource")]
    public void InternalServerError_ShouldLog(string path)
    {
        ILogger logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        Messages.AStarLog.Web.InternalServerError(logger, path);

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Is<EventId>(e => e.Id == 500),
            Arg.Any<object?>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object?, Exception?, string>>()
        );
    }

    [Theory]
    [InlineData("/api/resource")]
    public void BadGateway_ShouldLog(string path)
    {
        ILogger logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        Messages.AStarLog.Web.BadGateway(logger, path);

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Is<EventId>(e => e.Id == 502),
            Arg.Any<object?>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object?, Exception?, string>>()
        );
    }

    [Theory]
    [InlineData("/api/resource")]
    public void ServiceUnavailable_ShouldLog(string path)
    {
        ILogger logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        Messages.AStarLog.Web.ServiceUnavailable(logger, path);

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Is<EventId>(e => e.Id == 503),
            Arg.Any<object?>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object?, Exception?, string>>()
        );
    }

    [Theory]
    [InlineData("/api/resource")]
    public void GatewayTimeout_ShouldLog(string path)
    {
        ILogger logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        Messages.AStarLog.Web.GatewayTimeout(logger, path);

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Is<EventId>(e => e.Id == 504),
            Arg.Any<object?>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object?, Exception?, string>>()
        );
    }
}
