using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;

namespace AStar.Dev.OneDrive.Client.SettingsAndPreferences;

/// <summary>
/// Provides methods for loading and saving user settings and preferences.
/// </summary>
/// <remarks>Implementations of this interface manage the persistence of user-specific configuration data, such as
/// application preferences or settings. Methods may throw exceptions if the underlying storage is unavailable or if the
/// data is invalid. Thread safety and persistence location may vary by implementation.</remarks>
public interface ISettingsAndPreferencesService
{
    /// <summary>
    ///     Loads the user preferences from the file system.
    /// </summary>
    /// <returns>A <see cref="UserPreferences" /> object representing the loaded preferences.</returns>
    UserPreferences Load();

    /// <summary>
    ///     Saves the user preferences to the file system.
    /// </summary>
    /// <param name="userPreferences">The <see cref="UserPreferences" /> object to be saved.</param>
    void Save(UserPreferences userPreferences);
}
