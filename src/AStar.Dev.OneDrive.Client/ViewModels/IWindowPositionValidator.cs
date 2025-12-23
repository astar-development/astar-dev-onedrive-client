namespace AStar.Dev.OneDrive.Client.ViewModels;

/// <summary>
///     Provides validation services for window positioning and sizing.
/// </summary>
public interface IWindowPositionValidator
{
    /// <summary>
    ///     Validates whether the specified window dimensions are valid.
    /// </summary>
    /// <param name="width">The width of the window in pixels.</param>
    /// <param name="height">The height of the window in pixels.</param>
    /// <returns>True if both width and height are greater than zero; otherwise, false.</returns>
    bool IsValidSize(double width, double height);

    /// <summary>
    ///     Validates whether the specified window position coordinates are valid.
    /// </summary>
    /// <param name="x">The horizontal position of the window.</param>
    /// <param name="y">The vertical position of the window.</param>
    /// <returns>True if both x and y coordinates are greater than zero; otherwise, false.</returns>
    bool IsValidPosition(int x, int y);
}
