namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     The <see cref="SerilogConfig" /> class that contains the Serilog configuration.
/// </summary>
public sealed class SerilogConfig
{
    /// <summary>
    /// Gets or sets the Serilog configuration and logger instance used for structured logging within the application.
    /// </summary>
    /// <remarks>Configure this property to customize logging behavior, such as output sinks, log levels, and
    /// enrichment. Changing the Serilog instance affects how log events are processed and where they are
    /// written.</remarks>
    public Serilog Serilog { get; set; } = new();

    /// <summary>
    /// Gets or sets the logging configuration for the application.
    /// </summary>
    /// <remarks>Use this property to customize logging behavior, such as log levels, output destinations, and
    /// formatting options. Changes to the logging configuration take effect immediately and may impact how diagnostic
    /// information is recorded.</remarks>
    public Logging Logging { get; set; } = new();
}
