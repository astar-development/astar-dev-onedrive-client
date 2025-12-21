namespace AStar.Dev.Logging.Extensions;

/// <summary>
///     Interface for tracking telemetry events.
/// </summary>
public interface ITelemetryClient
{
    /// <summary>
    ///     Tracks a page view with the specified name.
    /// </summary>
    /// <param name="name"></param>
    void TrackPageView(string name);
}
