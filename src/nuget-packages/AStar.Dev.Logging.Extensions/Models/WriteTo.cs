namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     The <see cref="WriteTo" /> class that configures the relevant WriteTo Serilog logger.
/// </summary>
public sealed class WriteTo
{
    /// <summary>
    ///     The Name of the WriteTo option.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command-line arguments for the current operation.
    /// </summary>
    public Args Args { get; set; } = new();
}
