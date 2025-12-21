namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     The <see cref="Serilog" /> class used to configure Serilog.
/// </summary>
public sealed class Serilog
{
    /// <summary>
    /// Gets or sets the list of enrichment operations to apply to the data.
    /// </summary>
    /// <remarks>Each entry specifies an enrichment type or transformation that will be performed. The order
    /// of operations in the array determines the sequence in which enrichments are applied.</remarks>
    public string[] Enrich { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of sinks to which log events are written.
    /// </summary>
    /// <remarks>Each element in the array specifies a destination and configuration for log output. Common
    /// sinks include file, console, or external log servers. The order of sinks in the array determines the sequence in
    /// which log events are dispatched.</remarks>
    public WriteTo[] WriteTo { get; set; } = new WriteTo[] { new() { Args = new(), Name = "Seq" } };

    /// <summary>
    /// Gets or sets the minimum log level that must be met for log events to be recorded.
    /// </summary>
    /// <remarks>Adjust this property to control which log messages are processed based on their severity. Log
    /// events below the specified level will be ignored.</remarks>
    public MinimumLevel MinimumLevel { get; set; } = new();
}
