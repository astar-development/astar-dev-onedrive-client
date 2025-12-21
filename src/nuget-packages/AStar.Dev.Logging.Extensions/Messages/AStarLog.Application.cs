using Microsoft.Extensions.Logging;
using static AStar.Dev.Logging.Extensions.EventIds.AStarEventIds.Application;
namespace AStar.Dev.Logging.Extensions;

public static partial class AStarLog
{
    /// <summary>
    /// Provides logging methods for application lifecycle events, including startup, shutdown, and error conditions.
    /// </summary>
    /// <remarks>This class offers static methods to log key application events using structured logging. All
    /// methods require an <see cref="ILogger"/> instance and are intended to be used within application startup and
    /// shutdown routines. The logging methods use predefined event IDs and log levels to ensure consistent event
    /// tracking across the application. This class is thread-safe and can be used from any context where logging is
    /// required.</remarks>
    public static partial class Application
    {
        /// <summary>
        /// Logs an error for an Internal Server Error (500) at the specified path.
        /// </summary>
        /// <param name="logger">The logger instance to use.</param>
        /// <param name="appName">The name of the application that failed to start.</param>
        /// <param name="exceptionMessage">The exception message that caused the failure.</param>
        [LoggerMessage(EventId = FailedToStart, Level = LogLevel.Error, Message = "Failed to start application `{AppName}` - {ExceptionMessage}")]
        public static partial void ApplicationFailedToStart(ILogger logger, string appName, string exceptionMessage);

        /// <summary>
        /// Logs a debug message indicating that the application has started.
        /// </summary>
        /// <param name="logger">The logger instance to use.</param>
        /// <param name="appName">The name of the application that started.</param>
        [LoggerMessage(EventId = Started, Level = LogLevel.Debug, Message = "Started application `{AppName}`")]
        public static partial void ApplicationStarted(ILogger logger, string appName);

        /// <summary>
        /// Logs a debug message indicating that the application has stopped.
        /// </summary>
        /// <param name="logger">The logger instance to use.</param>
        /// <param name="appName">The name of the application that stopped.</param>
        [LoggerMessage(EventId = Stopped, Level = LogLevel.Debug, Message = "Stopped application `{AppName}`")]
        public static partial void ApplicationStopped(ILogger logger, string appName);
    }
}
