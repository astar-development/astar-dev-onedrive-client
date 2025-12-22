using Avalonia;

namespace AStar.Dev.OneDrive.Client.Views;

/// <summary>
///     Represents a window that can be positioned and sized.
/// </summary>
public interface IWindowPositionable
{
    /// <summary>
    ///     Gets or sets the width of the window.
    /// </summary>
    double Width { get; set; }

    /// <summary>
    ///     Gets or sets the height of the window.
    /// </summary>
    double Height { get; set; }

    /// <summary>
    ///     Gets or sets the position of the window.
    /// </summary>
    PixelPoint Position { get; set; }
}
