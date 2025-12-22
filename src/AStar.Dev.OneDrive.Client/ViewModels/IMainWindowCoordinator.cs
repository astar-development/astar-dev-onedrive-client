using AStar.Dev.OneDrive.Client.ViewModels;

namespace AStar.Dev.OneDrive.Client.Views;

/// <summary>
///     Defines the contract for coordinating actions and state between the main window
///     and its associated view model in the application.
/// </summary>
public interface IMainWindowCoordinator
{
    /// <summary>
    ///     Initializes the main window by applying user preferences, updating the associated view model,
    ///     and configuring the window's position and size.
    /// </summary>
    /// <param name="window">The main application window to be initialized.</param>
    /// <param name="mainWindowViewModel">The view model associated with the main application window.</param>
    void Initialize(IWindowPositionable window, MainWindowViewModel mainWindowViewModel);

    /// <summary>
    ///     Persists the current user preferences including window position, size, and UI settings
    ///     to durable storage for retrieval in future application sessions.
    /// </summary>
    /// <param name="window">The main application window whose state will be persisted.</param>
    /// <param name="vm">The view model containing the user preferences to persist.</param>
    void PersistUserPreferences(IWindowPositionable window, MainWindowViewModel vm);
}
