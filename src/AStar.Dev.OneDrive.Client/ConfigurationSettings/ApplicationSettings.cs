using System.Text.Json.Serialization;

namespace AStar.Dev.OneDrive.Client.ConfigurationSettings;

/// <summary>
///     Represents the application settings used for configuring the OneDrive client.
///     Provides properties to define various configuration parameters such as client identifiers,
///     download preferences, caching, paths, and scope definitions.
/// </summary>
public class ApplicationSettings
{
    /// <summary>
    ///   The configuration section name for application settings.
    /// </summary>
    internal const string SectionName = "AStarDevOneDriveClient";

    /// <summary>
    ///     Gets or sets the cache tag value used to manage the token cache serialization
    ///     and rotation mechanism for the OneDrive client. This property determines the
    ///     version of the cache file being utilized, ensuring isolation and preventing
    ///     conflicts when the cache is refreshed or rotated.
    /// </summary>
    public int CacheTag { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the version of the application. This value is used to
    ///     indicate the current release version of the software, often employed
    ///     for logging, user-facing information, or compatibility checks.
    /// </summary>
    public string ApplicationVersion { get; set; } = "1.0.0";

    /// <summary>
    ///     Gets or sets the user preferences path. This property is used to define
    ///     the directory where user preferences are stored.
    /// </summary>
    public string UserPreferencesPath { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the user preferences file name. This property is used to define
    ///     the name of the file where user preferences are stored.
    /// </summary>
    public string UserPreferencesFile { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the root path for OneDrive storage. This property is used to define
    ///     the base directory where OneDrive files are stored and accessed by the client.
    /// </summary>
    public string OneDriveRootPath { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the cache prefix used for naming cached items related to the OneDrive client.
    /// </summary>
    public string CachePrefix { get; set; } = string.Empty;

    /// <summary>
    ///     Gets the full path to the user preferences file, combining the base user preferences path
    ///     with the user preferences file name. This property is used to locate the specific file
    ///     where user preferences are stored.
    /// </summary>
    [JsonIgnore]
    internal string FullUserPreferencesPath
    {
        get => field ?? Path.Combine(BaseUserPreferencesPath, "user-preferences.json");
        set;
    }

    /// <summary>
    ///     Gets the base path for user preferences, combining the user's home folder with the
    ///     configured user preferences path. This property is used to locate the root directory
    ///     where user preferences are stored.
    /// </summary>
    [JsonIgnore]
    internal string BaseUserPreferencesPath
    {
        get => field ?? GetDefaultBaseUserPreferencesPath();
        set;
    }

    private static string GetDefaultBaseUserPreferencesPath()
        => OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "astar-dev", "astar-dev-onedrive-client")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "astar-dev", "astar-dev-onedrive-client");
}
