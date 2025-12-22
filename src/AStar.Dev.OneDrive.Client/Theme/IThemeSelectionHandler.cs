using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.SettingsAndPreferences;
using Avalonia.Controls;

namespace AStar.Dev.OneDrive.Client.Theme;

/// <summary>
///     Defines the contract for handling theme selection interactions in the user interface.
/// </summary>
public interface IThemeSelectionHandler
{
    /// <summary>
    ///     Initializes the theme selector control with the current user preferences and wires up event handlers.
    /// </summary>
    /// <param name="themeSelector">The ComboBox control used for theme selection.</param>
    /// <param name="preferences">The user preferences containing the current theme setting.</param>
    void Initialize(ComboBox themeSelector, UserPreferences preferences);

    /// <summary>
    ///     Updates the user preferences when the theme selection changes.
    /// </summary>
    /// <param name="selectedIndex">The index of the selected theme in the ComboBox.</param>
    /// <param name="preferences">The user preferences to update with the new theme selection.</param>
    void UpdatePreferenceOnChange(int selectedIndex, UserPreferences preferences);
}
