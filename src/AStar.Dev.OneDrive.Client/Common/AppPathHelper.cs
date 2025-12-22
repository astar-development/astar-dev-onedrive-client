using System.Runtime.InteropServices;

namespace AStar.Dev.OneDrive.Client.Common;

/// <summary>
///     Provides helper methods for determining application-specific data paths
///     across different operating systems. The class ensures that the application
///     data path is appropriately resolved based on the platform (Windows, macOS, or Linux).
/// </summary>
public static class AppPathHelper
{
    /// <summary>
    ///     Resolves the application data directory path for the specified application name
    ///     based on the current operating system. This ensures that application-specific
    ///     data can be stored in standard locations supported by the platform.
    /// </summary>
    /// <param name="appName">The name of the application whose data path needs to be resolved.</param>
    /// <returns>
    ///     A string representing the full path to the application data directory specific
    ///     to the provided application name. It varies based on the operating system:
    ///     Windows: "AppData\Roaming\
    ///     <appName>
    ///         "
    ///         macOS: "Library/Application Support/
    ///         <appName>
    ///             "
    ///             Linux/Unix: "~/.config/<appName>"
    /// </returns>
    public static string GetAppDataPath(string appName)
    {
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(baseDir, appName);
        }

        if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homeDir, "Library", "Application Support", appName);
        }
        else // Linux and other Unix
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homeDir, ".config", appName);
        }
    }

    /// <summary>
    ///     Gets the user's OS-specific home folder path.
    /// </summary>
    /// <returns>The full path to the user's home directory.</returns>
    public static string GetUserHomeFolder()
        => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
}
