// namespace AStar.Dev.OneDrive.Client.SettingsAndPreferences;

// /// <summary>
// ///     Represents the UI settings configured by the user.
// ///     It includes options for managing application behavior, appearance, and state persistence.
// /// </summary>
// public class UiSettings
// {
//     /// <summary>
//     ///     Gets or sets a value indicating whether files should be automatically downloaded
//     ///     after the synchronization process completes.
//     /// </summary>
//     /// <remarks>
//     ///     When set to <c>true</c>, the application will attempt to download files immediately
//     ///     following a successful sync operation. This setting is primarily intended to enhance the
//     ///     user experience by ensuring that the most recent versions of files are readily available locally.
//     ///     The behavior of this property can be configured through user preferences.
//     /// </remarks>
//     public bool DownloadFilesAfterSync { get; set; }

//     /// <summary>
//     ///     Gets or sets a value indicating whether files should be automatically uploaded
//     ///     after the synchronization process completes.
//     /// </summary>
//     /// <remarks>
//     ///     When set to <c>true</c>, the application will attempt to upload files immediately
//     ///     following a successful sync operation. This setting is designed to ensure that local changes
//     ///     are promptly reflected in the cloud storage, maintaining up-to-date versions of files.
//     ///     Users can configure this behavior according to their preferences for seamless file updates.
//     /// </remarks>
//     public bool UploadFilesAfterSync { get; set; }

//     /// <summary>
//     ///     Gets or sets a value indicating whether the application should retain the user's authentication state across sessions.
//     /// </summary>
//     /// <remarks>
//     ///     When set to <c>true</c>, the user will remain signed in even after restarting the application,
//     ///     provided that the authentication token remains valid. This property is often used to enhance
//     ///     user convenience by avoiding repeated sign-in prompts. If <c>false</c>, the user will be signed out
//     ///     at the end of the session.
//     /// </remarks>
//     public bool RememberMe { get; set; } = true;

//     /// <summary>
//     ///     Gets or sets the UI theme preference selected by the user.
//     /// </summary>
//     /// <remarks>
//     ///     This property determines the visual appearance of the application's user interface.
//     ///     It supports values such as "Light", "Dark", and "Auto", where "Auto" allows the theme
//     ///     to dynamically adapt based on the system's theme settings.
//     ///     The selected theme is applied throughout the application and can be modified via user preferences.
//     /// </remarks>
//     public string Theme { get; set; } = "Auto";

//     /// <summary>
//     ///     Gets or sets the description of the most recent action performed by the user or system.
//     /// </summary>
//     /// <remarks>
//     ///     This property is intended to store and reflect the last significant action taken within
//     ///     the application, such as a synchronization, data upload, or UI interaction. It provides
//     ///     context for the user or system regarding recent events and can be used to update status
//     ///     displays within the UI.
//     ///     Defaults to "No action yet" when no actions have been recorded.
//     /// </remarks>
//     public string LastAction { get; set; } = "No action yet";

//     /// <summary>
//     ///     Gets or sets the maximum number of parallel download operations that can be
//     ///     performed concurrently. This property is used to control the level of
//     ///     concurrency when retrieving files from OneDrive, helping to manage system
//     ///     resource usage effectively.
//     /// </summary>
//     public int MaxParallelDownloads { get; set; } = 8;

//     /// <summary>
//     ///     Gets or sets the maximum number of items to be retrieved or processed in a single batch during
//     ///     download operations. This value is used to optimize data retrieval by controlling the batch size
//     ///     for network requests or processing chunks. Adjusting this property can balance performance and resource usage.
//     /// </summary>
//     public int DownloadBatchSize { get; set; } = 100;

//     /// <summary>
//     ///     Updates the current instance of <see cref="UiSettings" /> with values from another
//     ///     <see cref="UiSettings" /> instance.
//     /// </summary>
//     /// <param name="other">
//     ///     The instance of <see cref="UiSettings" /> whose values
//     ///     will be copied to the current instance.
//     /// </param>
//     /// <returns>Returns the updated instance of <see cref="UiSettings" />.</returns>
//     public UiSettings Update(UiSettings other)
//     {
//         MaxParallelDownloads = other.MaxParallelDownloads;
//         DownloadBatchSize = other.DownloadBatchSize;
//         LastAction = other.LastAction;
//         Theme = other.Theme;
//         RememberMe = other.RememberMe;
//         DownloadFilesAfterSync = other.DownloadFilesAfterSync;
//         UploadFilesAfterSync = other.UploadFilesAfterSync;

//         return this;
//     }
// }
