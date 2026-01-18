namespace AStar.Dev.OneDrive.Client.Core.Entities;

/// <summary>
/// Represents window position and size preferences.
/// </summary>
/// <param name="Id">Unique identifier (should always be 1 for singleton).</param>
/// <param name="X">Window X position (null if maximized or first run).</param>
/// <param name="Y">Window Y position (null if maximized or first run).</param>
/// <param name="Width">Window width in pixels.</param>
/// <param name="Height">Window height in pixels.</param>
/// <param name="IsMaximized">Indicates whether the window is maximized.</param>
public sealed record WindowPreferences(
    int Id,
    double? X,
    double? Y,
    double Width,
    double Height,
    bool IsMaximized
);
