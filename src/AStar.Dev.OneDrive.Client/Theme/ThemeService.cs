using AStar.Dev.OneDrive.Client.SettingsAndPreferences;
using Avalonia;
using Avalonia.Styling;

namespace AStar.Dev.OneDrive.Client.Theme;

/// <summary>
///     Provides services for applying themes based on user preferences.
///     This service is responsible for setting the application theme
///     (e.g., Light, Dark, or Default) by interacting with the application instance.
/// </summary>
public class ThemeService
{
    /// <summary>
    ///     Applies the theme preference based on the user's settings.
    /// </summary>
    /// <param name="userPreferences">The user's preferences containing UI settings, including the desired theme.</param>
    public void ApplyThemePreference(UserPreferences userPreferences)
    {
        if(Application.Current is not App app)
            return;

        switch(userPreferences.UiSettings.Theme)
        {
            case "Light":
                app.SetTheme(ThemeVariant.Light);
                break;
            case "Dark":
                app.SetTheme(ThemeVariant.Dark);
                break;
            default:
                app.SetTheme(ThemeVariant.Default);
                break;
        }
    }
}
