using AStar.Dev.OneDrive.Client.FromV3.Models;

namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Service for managing window position and size preferences.
/// </summary>
public interface IWindowPreferencesService
{
    /// <summary>
    /// Loads the window preferences from storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The window preferences, or null if none exist.</returns>
    Task<WindowPreferences?> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the window preferences to storage.
    /// </summary>
    /// <param name="preferences">The preferences to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(WindowPreferences preferences, CancellationToken cancellationToken = default);
}
