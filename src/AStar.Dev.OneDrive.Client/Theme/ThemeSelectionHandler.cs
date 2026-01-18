using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using Avalonia.Controls;

namespace AStar.Dev.OneDrive.Client.Theme;

/// <summary>
///     Handles theme selection interactions by coordinating between the UI control,
///     user preferences, and theme application services.
/// </summary>
public class ThemeSelectionHandler(IThemeMapper themeMapper, IThemeService themeService) : IThemeSelectionHandler
{
    /// <inheritdoc />
    public void Initialize(ComboBox themeSelector, UserPreferences userPreferences)
    {
        themeSelector.SelectedIndex = themeMapper.MapThemeToIndex(userPreferences.UiSettings.Theme);
        themeSelector.SelectionChanged += (s, _) => OnSelectionChanged(s, userPreferences);
        themeService.ApplyThemePreference(userPreferences);
    }

    /// <inheritdoc />
    public void UpdatePreferenceOnChange(int selectedIndex, UserPreferences userPreferences)
    {
        var selectedTheme = themeMapper.MapIndexToTheme(selectedIndex);
        userPreferences.UiSettings.Theme = selectedTheme;
        themeService.ApplyThemePreference(userPreferences);
    }

    private void OnSelectionChanged(object? sender, UserPreferences preferences)
    {
        if(sender is not ComboBox comboBox)
            return;

        UpdatePreferenceOnChange(comboBox.SelectedIndex, preferences);
    }
}
