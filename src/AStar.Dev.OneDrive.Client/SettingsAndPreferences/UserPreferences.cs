// namespace AStar.Dev.OneDrive.Client.SettingsAndPreferences;

// /// <summary>
// ///     Represents the preferences of a user, including settings related to the application window
// ///     and the user interface. This class serves as a container for various user-configurable
// ///     settings that inform application behavior and UI presentation.
// /// </summary>
// public class UserPreferences
// {
//     /// <summary>
//     ///     Represents the configuration settings for an application window, including its dimensions
//     ///     and position on the screen. These settings are used to define and persist the window's
//     ///     size and location between application sessions.
//     ///     Properties defined in the class include:
//     ///     - <c>WindowWidth</c>: Specifies the width of the window.
//     ///     - <c>WindowHeight</c>: Specifies the height of the window.
//     ///     - <c>WindowX</c>: Specifies the X-coordinate of the window's top-left corner on the screen.
//     ///     - <c>WindowY</c>: Specifies the Y-coordinate of the window's top-left corner on the screen.
//     ///     This class is typically used in conjunction with user preferences to store and restore the
//     ///     window's state, ensuring a consistent user experience.
//     /// </summary>
//     public WindowSettings WindowSettings { get; set; } = new();

//     /// <summary>
//     ///     Represents the configuration settings related to the user interface. This class provides
//     ///     a container for preferences and settings that define how the user interface behaves and appears.
//     ///     These settings can influence aspects such as the theme, display preferences, and other
//     ///     UI-related customizations.
//     ///     Properties contained within the class may include:
//     ///     - Theme: Determines the appearance of the application, such as "Light" or "Dark" mode.
//     ///     - DownloadFilesAfterSync: Specifies whether files should be downloaded automatically
//     ///     after synchronization is completed.
//     ///     - RememberMe: Indicates whether to persist the user session for future application use.
//     ///     This class is typically used to store and retrieve UI-related preferences, enabling the
//     ///     application to provide a personalized and consistent user experience.
//     /// </summary>
//     public UiSettings UiSettings { get; set; } = new();

//     /// <summary>
//     ///     Updates the current instance of <see cref="UserPreferences" /> with values from another
//     ///     <see cref="UserPreferences" /> instance.
//     /// </summary>
//     /// <param name="other">
//     ///     The instance of <see cref="UserPreferences" /> whose values
//     ///     will be copied to the current instance.
//     /// </param>
//     /// <returns>Returns the updated instance of <see cref="UserPreferences" />.</returns>
//     public UserPreferences Update(UserPreferences other)
//     {
//         UiSettings = UiSettings.Update(other.UiSettings);
//         WindowSettings = WindowSettings.Update(other.WindowSettings);
//         return this;
//     }
// }
