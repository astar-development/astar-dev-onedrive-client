namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     The <see cref="MinimumLevel" /> class used to configure Serilog.
/// </summary>
public sealed class MinimumLevel
{
    /// <summary>
    ///     The default logging level.
    /// </summary>
    public string Default { get; set; } = string.Empty;

    /// <summary>
    ///     The <see cref="Override" /> class used to override the standard logging levels.
    /// </summary>
    public Override Override { get; set; } = new();
}
