using System.Text.Json.Serialization;

namespace AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;

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
    public string UserPreferencesFile { get; set; } = "user-preferences.json";

    /// <summary>
    /// Gets or sets the name of the database file used for synchronization.
    /// </summary>
    public string DatabaseName { get; set; } = "onedrive-sync.db";

    /// <summary>
    ///     Gets or sets the root path for OneDrive storage. This property is used to define
    ///     the base directory where OneDrive files are stored and accessed by the client.
    /// </summary>
    public string OneDriveRootDirectory { get; set; } = "OneDrive-Sync";

    /// <summary>
    ///     Gets or sets the cache prefix used for naming cached items related to the OneDrive client.
    /// </summary>
    public string CachePrefix { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "http://localhost";
    public string GraphUri { get; set; } = "https://graph.microsoft.com/v1.0/me/drive";

    /// <summary>
    ///     Gets the full path to the user preferences file, combining the base user preferences path
    ///     with the user preferences file name. This property is used to locate the specific file
    ///     where user preferences are stored.
    /// </summary>
    [JsonIgnore]
    public string FullUserPreferencesPath
        => Path.Combine(FullUserPreferencesDirectory, UserPreferencesFile);

    [JsonIgnore]
    public static string FullUserPreferencesDirectory
        => Path.Combine(AppPathHelper.GetAppDataPath(ApplicationName));

    [JsonIgnore]
    public string FullDatabasePath
        => Path.Combine(FullDatabaseDirectory, DatabaseName);

    [JsonIgnore]
    public static string FullDatabaseDirectory
        => Path.Combine(AppPathHelper.GetAppDataPath(ApplicationName), "database");

    [JsonIgnore]
    public string FullUserSyncPath
        => Path.Combine(AppPathHelper.GetUserHomeFolder(), OneDriveRootDirectory);

    private static readonly string ApplicationName = "astar-dev-onedrive-client";
}
