using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;

namespace AStar.Dev.OneDrive.Client.Theme;

/// <summary>
///     Defines the contract for services that apply themes based on user preferences.
/// </summary>
public interface IThemeService
{
    /// <summary>
    ///     Applies the theme preference based on the user's settings.
    /// </summary>
    /// <param name="userPreferences">The user's preferences containing UI settings, including the desired theme.</param>
    void ApplyThemePreference(UserPreferences userPreferences);
}
