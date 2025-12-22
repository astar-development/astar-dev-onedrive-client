namespace AStar.Dev.OneDrive.Client.Theme;

/// <summary>
///     Provides bidirectional mapping between theme string representations and their corresponding
///     numeric indices for UI controls.
/// </summary>
public class ThemeMapper : IThemeMapper
{
    /// <inheritdoc />
    public int MapThemeToIndex(string theme)
        => theme switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0
        };

    /// <inheritdoc />
    public string MapIndexToTheme(int index)
        => index switch
        {
            1 => "Light",
            2 => "Dark",
            _ => "Auto"
        };
}
