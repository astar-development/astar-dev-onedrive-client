namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     The <see cref="Logging" /> class used to configure the standard logging.
/// </summary>
public sealed class Logging
{
    /// <summary>
    ///     An instance of <see cref="Console" /> class used to configure the Console logger.
    /// </summary>
    public Console Console { get; set; } = new();

    /// <summary>
    ///     An instance of <see cref="ApplicationInsights" /> class used to configure the Application Insights logger.
    /// </summary>
    public ApplicationInsights ApplicationInsights { get; set; } = new();
}
