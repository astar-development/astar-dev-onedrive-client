namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     The <see cref="Console" /> class used by Serilog Console section of the logging configuration.
/// </summary>
public sealed class Console
{
    /// <summary>
    ///     The Formatter Name to use.
    /// </summary>
    public string FormatterName { get; set; } = string.Empty;

    /// <summary>
    ///     The <see cref="FormatterOptions" /> class used to supply the Formatter Options to use.
    /// </summary>
    public FormatterOptions FormatterOptions { get; set; } = new();
}
