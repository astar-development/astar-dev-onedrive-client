namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     The <see cref="LogLevel" /> class used to configure Application Insights.
/// </summary>
public sealed class LogLevel
{
    /// <summary>
    ///     The default logging level.
    /// </summary>
    public string Default { get; set; } = string.Empty;

    /// <summary>
    ///     The Microsoft AspNetCore logging level.
    /// </summary>
    public string MicrosoftAspNetCore { get; set; } = string.Empty;

    /// <summary>
    ///     The AStar-specific logging level.
    /// </summary>
    public string AStar { get; set; } = string.Empty;
}
