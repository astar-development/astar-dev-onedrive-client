// using Avalonia;

// namespace AStar.Dev.OneDrive.Client.SettingsAndPreferences;

// /// <summary>
// ///     Represents settings related to the configuration and positioning of a window.
// /// </summary>
// public class WindowSettings
// {
//     /// <summary>
//     ///     Gets or sets the width of the window in pixels.
//     ///     This property defines or persists the horizontal size of the application window.
//     /// </summary>
//     public double WindowWidth { get; set; } = 1000;

//     /// <summary>
//     ///     Gets or sets the height of the window in pixels.
//     ///     This property helps to define or persist the vertical size of the application window.
//     /// </summary>
//     public double WindowHeight { get; set; } = 800;

//     /// <summary>
//     ///     Represents the horizontal position of the window relative to the screen.
//     /// </summary>
//     public int WindowX { get; set; } = 100;

//     /// <summary>
//     ///     Gets or sets the Y-coordinate of the window position.
//     /// </summary>
//     public int WindowY { get; set; } = 100;

//     /// <summary>
//     ///     Updates the current instance of <see cref="WindowSettings" /> with values from another
//     ///     <see cref="WindowSettings" /> instance.
//     /// </summary>
//     /// <param name="other">
//     ///     The instance of <see cref="WindowSettings" /> whose values
//     ///     will be copied to the current instance.
//     /// </param>
//     /// <returns>Returns the updated instance of <see cref="WindowSettings" />.</returns>
//     public WindowSettings Update(WindowSettings other)
//     {
//         WindowHeight = other.WindowHeight;
//         WindowWidth = other.WindowWidth;
//         WindowX = other.WindowX;
//         WindowY = other.WindowY;

//         return this;
//     }

//     public WindowSettings Update(PixelPoint windowPosition, double windowWidth, double windowHeight)
//     {
//         WindowHeight = windowHeight;
//         WindowWidth = windowWidth;
//         WindowX = windowPosition.X;
//         WindowY = windowPosition.Y;

//         return this;
//     }
// }
