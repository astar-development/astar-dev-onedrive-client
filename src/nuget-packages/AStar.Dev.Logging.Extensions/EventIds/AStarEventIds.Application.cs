using Microsoft.Extensions.Logging;

namespace AStar.Dev.Logging.Extensions.EventIds;

public static partial class AStarEventIds
{
    /// <summary>
    /// Provides preconfigured <see cref="EventId"/> values for logging application lifecycle events such as start and
    /// stopped.
    /// </summary>
    /// <remarks>Use the <see cref="Started"/> and <see cref="Stopped"/> event IDs when logging application startup
    /// and shutdown events to ensure consistent event identification across logs.</remarks>
    public static partial class Application
    {
        /// <summary>
        ///     Gets the <see cref="EventId" /> preconfigured for logging application start
        /// </summary>
        public const int Started = 10_000;

        /// <summary>
        ///     Gets the <see cref="EventId" /> preconfigured for logging application stop
        /// </summary>
        public const int Stopped = 10_001;

        /// <summary>
        ///     Gets the <see cref="EventId" /> preconfigured for logging application failure to start
        /// </summary>
        public const int FailedToStart = 10_002;
    }
}
