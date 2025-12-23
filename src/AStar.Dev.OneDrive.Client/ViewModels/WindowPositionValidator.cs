namespace AStar.Dev.OneDrive.Client.ViewModels;

/// <summary>
///     Validates window positioning and sizing constraints to ensure windows are positioned
///     within acceptable screen boundaries and have valid dimensions.
/// </summary>
public class WindowPositionValidator : IWindowPositionValidator
{
    /// <inheritdoc />
    public bool IsValidSize(double width, double height)
        => width > 0 && height > 0;

    /// <inheritdoc />
    public bool IsValidPosition(int x, int y)
        => x > 0 && y > 0;
}
