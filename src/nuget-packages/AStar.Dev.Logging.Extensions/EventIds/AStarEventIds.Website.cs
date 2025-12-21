using Microsoft.Extensions.Logging;

namespace AStar.Dev.Logging.Extensions.EventIds;

public static partial class AStarEventIds
{
    /// <summary>
    ///     Contains the <see cref="EventId" /> events for website-related logging.
    /// </summary>
    public static partial class Website
    {
        /// <summary>
        ///     Gets the <see cref="EventId" /> preconfigured for logging a page view
        /// </summary>
        public const int PageView = 1_000;
    }
}
