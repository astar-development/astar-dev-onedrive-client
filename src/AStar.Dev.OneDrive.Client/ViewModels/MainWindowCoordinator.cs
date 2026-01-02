using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.SettingsAndPreferences;
using AStar.Dev.OneDrive.Client.Theme;
using Avalonia;

namespace AStar.Dev.OneDrive.Client.ViewModels;

/// <summary>
///     Coordinates the initialization, state management, and user preferences for the main application window.
///     Provides methods to initialize the window and its associated view model, and persist user preferences.
/// </summary>
public class MainWindowCoordinator(ISettingsAndPreferencesService settingsAndPreferencesService, IThemeService themeService, IWindowPositionValidator positionValidator) : IMainWindowCoordinator
{
    /// <inheritdoc />
    public void Initialize(IWindowPositionable window, MainWindowViewModel mainWindowViewModel)
    {
        UserPreferences userPreferences = settingsAndPreferencesService.Load();

        themeService.ApplyThemePreference(userPreferences);

        mainWindowViewModel.UserPreferences = userPreferences;
        mainWindowViewModel.SyncStatusMessage = userPreferences.UiSettings.LastAction;

        ApplyWindowSettings(window, userPreferences.WindowSettings);
    }

    /// <inheritdoc />
    public void PersistUserPreferences(IWindowPositionable window, MainWindowViewModel mainWindowViewModel)
    {
        _ = mainWindowViewModel.UserPreferences.WindowSettings.Update(window.Position, window.Width, window.Height);
        settingsAndPreferencesService.Save(mainWindowViewModel.UserPreferences);
    }

    private void ApplyWindowSettings(IWindowPositionable window, WindowSettings settings)
    {
        if(positionValidator.IsValidSize(settings.WindowWidth, settings.WindowHeight))
        {
            window.Width = settings.WindowWidth;
            window.Height = settings.WindowHeight;
        }

        if(positionValidator.IsValidPosition(settings.WindowX, settings.WindowY))
            window.Position = new PixelPoint(settings.WindowX, settings.WindowY);
    }
}
