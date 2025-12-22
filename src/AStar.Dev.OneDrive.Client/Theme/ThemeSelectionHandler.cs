using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.SettingsAndPreferences;
using Avalonia.Controls;

namespace AStar.Dev.OneDrive.Client.Theme;

/// <summary>
///     Handles theme selection interactions by coordinating between the UI control,
///     user preferences, and theme application services.
/// </summary>
public class ThemeSelectionHandler(IThemeMapper themeMapper, ThemeService themeService) : IThemeSelectionHandler
{
    /// <inheritdoc />
    public void Initialize(ComboBox themeSelector, UserPreferences preferences)
    {
        themeSelector.SelectedIndex = themeMapper.MapThemeToIndex(preferences.UiSettings.Theme);
        themeSelector.SelectionChanged += (s, _) => OnSelectionChanged(s, preferences);
    }

    /// <inheritdoc />
    public void UpdatePreferenceOnChange(int selectedIndex, UserPreferences preferences)
    {
        var selectedTheme = themeMapper.MapIndexToTheme(selectedIndex);
        preferences.UiSettings.Theme = selectedTheme;
        themeService.ApplyThemePreference(preferences);
    }

    private void OnSelectionChanged(object? sender, UserPreferences preferences)
    {
        if(sender is not ComboBox comboBox)
            return;

        UpdatePreferenceOnChange(comboBox.SelectedIndex, preferences);
    }
}
