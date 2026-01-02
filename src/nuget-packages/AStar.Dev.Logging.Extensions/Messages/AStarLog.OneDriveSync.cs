using Microsoft.Extensions.Logging;
using static AStar.Dev.Logging.Extensions.EventIds.AStarEventIds.OneDriveSync;

namespace AStar.Dev.Logging.Extensions.Messages;

/// <summary>
/// Provides strongly-typed logging methods for common HTTP status codes and errors in AStar applications.
/// </summary>
public static partial class AStarLog
{
    /// <summary>
    /// Provides static methods for logging synchronizing files and folders with Microsoft OneDrive.
    /// </summary>
    /// <remarks>This class serves as the entry point for logging OneDrive synchronization operations.</remarks>
    public static partial class OneDriveSync
    {
        /// <summary>
        /// Logs an informational message indicating that a OneDrive synchronization has started for the specified
        /// application.
        /// </summary>
        /// <param name="logger">The logger instance used to write the log message.</param>
        /// <param name="startTimeUtc">The UTC timestamp representing when the synchronization started.</param>
        [LoggerMessage(EventId = Started, Level = LogLevel.Information, Message = "Started OneDriveSync - {StartTimeUtc}")]
        public static partial void FullSyncStarted(ILogger logger, DateTimeOffset startTimeUtc);

    }
}
