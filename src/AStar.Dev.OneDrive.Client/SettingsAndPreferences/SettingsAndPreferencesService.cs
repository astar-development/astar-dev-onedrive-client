using System.IO.Abstractions;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.Utilities;

namespace AStar.Dev.OneDrive.Client.SettingsAndPreferences;

/// <inheritdoc/>
public class SettingsAndPreferencesService(IFileSystem fileSystem, ApplicationSettings appSettings) : ISettingsAndPreferencesService
{
    /// <inheritdoc/>
    public UserPreferences Load()
        => fileSystem.File.ReadAllText(appSettings.FullUserPreferencesPath).FromJson<UserPreferences>();

    /// <inheritdoc/>
    public void Save(UserPreferences userPreferences) => fileSystem.File.WriteAllText(appSettings.FullUserPreferencesPath, userPreferences.ToJson());
}
