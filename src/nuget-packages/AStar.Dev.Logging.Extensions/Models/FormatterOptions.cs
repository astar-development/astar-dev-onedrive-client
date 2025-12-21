namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     The <see cref="FormatterOptions" /> class used by the Serilog Console logger.
/// </summary>
public sealed class FormatterOptions
{
    /// <summary>
    ///     Whether to use a single line for the logging.
    /// </summary>
    public bool SingleLine { get; set; }

    /// <summary>
    ///     Whether to include the Scopes for the logging.
    /// </summary>
    public bool IncludeScopes { get; set; }

    /// <summary>
    ///     The Timestamp Format to use. Defaults to "HH:mm:ss "
    /// </summary>
    public string TimestampFormat { get; set; } = "HH:mm:ss ";

    /// <summary>
    ///     Whether to use a UTC Timestamp or not. The default is <see langword="true" />.
    /// </summary>
    public bool UseUtcTimestamp { get; set; } = true;

    /// <summary>
    ///     The <see cref="JsonWriterOptions" /> used to configure the JSON Writer.
    /// </summary>
    public JsonWriterOptions JsonWriterOptions { get; set; } = new();
}
