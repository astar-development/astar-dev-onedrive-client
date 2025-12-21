namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     The <see cref="Override" /> class used to configure the Serilog logging level overrides.
/// </summary>
public sealed class Override
{
    /// <summary>
    /// Gets or sets the MinimumLevel logging value for the MicrosoftAspNetCore namespace.
    /// </summary>
    public string MicrosoftAspNetCore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MinimumLevel logging value for the System.Net.Http namespace.
    /// </summary>
    public string SystemNetHttp { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MinimumLevel logging value for the AStar Development namespace.
    /// </summary>
    public string AStar { get; set; } = string.Empty;
}
