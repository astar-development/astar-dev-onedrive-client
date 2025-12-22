using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.SettingsAndPreferences;
using AStar.Dev.OneDrive.Client.Theme;
using AStar.Dev.OneDrive.Client.ViewModels;
using Avalonia;

namespace AStar.Dev.OneDrive.Client.Views;

/// <summary>
///     Coordinates the initialization, state management, and user preferences for the main application window.
///     Provides methods to initialize the window and its associated view model, and persist user preferences.
/// </summary>
public class MainWindowCoordinator(ISettingsAndPreferencesService settingsAndPreferencesService, ThemeService themeService, IWindowPositionValidator positionValidator) : IMainWindowCoordinator
{
    /// <inheritdoc />
    public void Initialize(IWindowPositionable window, MainWindowViewModel mainWindowViewModel)
    {
        UserPreferences userPreferences = settingsAndPreferencesService.Load();

        themeService.ApplyThemePreference(userPreferences);

        mainWindowViewModel.UserPreferences = userPreferences;
        mainWindowViewModel.SyncStatus = userPreferences.UiSettings.LastAction;

        ApplyWindowSettings(window, userPreferences.WindowSettings);
    }

    /// <inheritdoc />
    public void PersistUserPreferences(IWindowPositionable window, MainWindowViewModel vm)
    {
        _ = vm.UserPreferences.WindowSettings.Update(window.Position, window.Width, window.Height);
        settingsAndPreferencesService.Save(vm.UserPreferences);
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
