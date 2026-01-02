using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using AStar.Dev.Source.Generators.Attributes;

namespace AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;

/// <summary>
///     Represents the application settings used for configuring the OneDrive client.
///     Provides properties to define various configuration parameters such as client identifiers,
///     download preferences, caching, paths, and scope definitions.
/// </summary>
[AutoRegisterOptions]
public class ApplicationSettings
{
    /// <summary>
    ///   The configuration section name for application settings.
    /// </summary>
    public const string SectionName = "AStarDevOneDriveClient";

    /// <summary>
    ///     Gets or sets the cache tag value used to manage the token cache serialization
    ///     and rotation mechanism for the OneDrive client. This property determines the
    ///     version of the cache file being utilized, ensuring isolation and preventing
    ///     conflicts when the cache is refreshed or rotated.
    /// </summary>
    [Required]
    [Range(1, 100, ErrorMessage = "CacheTag must be greater than 0.")]
    public int CacheTag { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the version of the application. This value is used to
    ///     indicate the current release version of the software, often employed
    ///     for logging, user-facing information, or compatibility checks.
    /// </summary>
    [Required]
    [RegularExpression(@"^\d+\.\d+\.\d+(-[A-Za-z0-9]+)?$", ErrorMessage = "ApplicationVersion must follow semantic versioning (e.g., 1.0.0 or 1.0.0-beta).")]
    public string ApplicationVersion { get; set; } = "1.0.0";

    /// <summary>
    ///     Gets or sets the user preferences path. This property is used to define
    ///     the directory where user preferences are stored.
    /// </summary>
    [Required]
    public string UserPreferencesPath { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the user preferences file name. This property is used to define
    ///     the name of the file where user preferences are stored.
    /// </summary>
    [Required]
    public string UserPreferencesFile { get; set; } = "user-preferences.json";

    /// <summary>
    /// Gets or sets the name of the database file used for synchronization.
    /// </summary>
    [Required]
    public string DatabaseName { get; set; } = "onedrive-sync.db";

    /// <summary>
    ///     Gets or sets the root path for OneDrive storage. This property is used to define
    ///     the base directory where OneDrive files are stored and accessed by the client.
    /// </summary>
    [Required]
    public string OneDriveRootDirectory { get; set; } = "OneDrive-Sync";

    /// <summary>
    ///     Gets or sets the cache prefix used for naming cached items related to the OneDrive client.
    /// </summary>
    [Required]
    public string CachePrefix { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URI to which the authentication response is redirected.
    /// </summary>
    /// <remarks>The redirect URI must be a valid absolute URI. This property is typically used in OAuth or
    /// OpenID Connect authentication flows to specify where the authorization server should send the user after
    /// authentication. Ensure that the value matches the redirect URI registered with the authentication
    /// provider.</remarks>
    [Required]
    [RegularExpression(@"^https?://.+", ErrorMessage = "RedirectUri must be a valid absolute URI starting with http:// or https://")]
    public string RedirectUri { get; set; } = "http://localhost";

    /// <summary>
    /// Gets or sets the URI of the Microsoft Graph endpoint to use for API requests.
    /// </summary>
    /// <remarks>The default value targets the signed-in user's OneDrive. Set this property to specify a
    /// different Microsoft Graph resource or version as needed.</remarks>
    [Required]
    public string GraphUri { get; set; } = "https://graph.microsoft.com/v1.0/me/drive";

    /// <summary>
    ///     Gets the full path to the user preferences file, combining the base user preferences path
    ///     with the user preferences file name. This property is used to locate the specific file
    ///     where user preferences are stored.
    /// </summary>
    [JsonIgnore]
    public string FullUserPreferencesPath
        => Path.Combine(FullUserPreferencesDirectory, UserPreferencesFile);

    /// <summary>
    /// Gets the full path to the directory where user preferences are stored for the application.
    /// </summary>
    /// <remarks>The returned path is based on the application's name and the current user's profile. The
    /// directory may not exist until it is created by the application.</remarks>
    [JsonIgnore]
    public static string FullUserPreferencesDirectory
        => Path.Combine(AppPathHelper.GetAppDataPath(ApplicationName));

    /// <summary>
    /// Gets the full file system path to the database, including the directory and file name.
    /// </summary>
    [JsonIgnore]
    public string FullDatabasePath
        => Path.Combine(FullDatabaseDirectory, DatabaseName);

    /// <summary>
    /// Gets the full file system path to the application's database directory.
    /// </summary>
    /// <remarks>The returned path is constructed by combining the application's data directory with the
    /// "database" subdirectory. This property is intended for use when accessing or managing files within the
    /// application's database folder.</remarks>
    [JsonIgnore]
    public static string FullDatabaseDirectory
        => Path.Combine(AppPathHelper.GetAppDataPath(ApplicationName), "database");

    /// <summary>
    /// Gets the full file system path to the user's OneDrive synchronization directory.
    /// </summary>
    [JsonIgnore]
    public string FullUserSyncPath
        => Path.Combine(AppPathHelper.GetUserHomeFolder(), OneDriveRootDirectory);

    private static readonly string ApplicationName = "astar-dev-onedrive-client";
}
