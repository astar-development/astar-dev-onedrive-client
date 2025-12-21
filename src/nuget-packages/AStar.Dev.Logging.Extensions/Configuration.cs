namespace AStar.Dev.Logging.Extensions;

/// <summary>
///     The <see cref="Configuration" /> class is a container for any applicable constants to use during logging
///     configuration.
/// </summary>
public static class Configuration
{
    /// <summary>
    ///     Returns the default value for the External Settings File used to configure the logging extensions.
    /// </summary>
    public static string ExternalSettingsFile => "astar-logging-settings.json";
}
