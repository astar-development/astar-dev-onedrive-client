namespace AStar.Dev.OneDrive.Client.Theme;

/// <summary>
///     Provides bidirectional mapping between theme string representations and their corresponding
///     numeric indices for UI controls such as combo boxes.
/// </summary>
public interface IThemeMapper
{
    /// <summary>
    ///     Maps a theme name to its corresponding numeric index for UI control binding.
    /// </summary>
    /// <param name="theme">The theme name (e.g., "Light", "Dark", "Auto").</param>
    /// <returns>The numeric index corresponding to the theme: 0 for Auto, 1 for Light, 2 for Dark.</returns>
    int MapThemeToIndex(string theme);

    /// <summary>
    ///     Maps a numeric index from a UI control to its corresponding theme name.
    /// </summary>
    /// <param name="index">The numeric index from the UI control.</param>
    /// <returns>The theme name corresponding to the index: "Auto" for 0, "Light" for 1, "Dark" for 2.</returns>
    string MapIndexToTheme(int index);
}
