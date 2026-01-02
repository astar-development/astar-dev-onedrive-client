using Microsoft.Extensions.Logging;

namespace AStar.Dev.Logging.Extensions.EventIds;

/// <summary>
///     The <see cref="AStarEventIds" /> class contains the defined <see cref="EventId" /> events available for logging
///     Stand-alone <see cref="EventId" /> events can be defined but care should be taken to avoid reusing the values used here
/// </summary>
public static partial class AStarEventIds
{
    /// <summary>
    /// Provides the event identifier used for logging the start of a OneDrive synchronization operation.
    /// </summary>
    /// <remarks>Use this constant when emitting log entries to indicate that a OneDriveSync process has
    /// begun. This value is intended for integration with structured logging systems that support event IDs, such as
    /// Microsoft.Extensions.Logging.</remarks>
    public static partial class OneDriveSync
    {
        /// <summary>
        ///     Gets the <see cref="EventId" /> preconfigured for logging OneDriveSync start
        /// </summary>
        public const int Started = 20_000;
    }
}
