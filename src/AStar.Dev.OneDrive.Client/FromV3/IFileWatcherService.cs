using AStar.Dev.OneDrive.Client.FromV3.Models;

namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Service for monitoring local file system changes to trigger synchronization.
/// </summary>
/// <remarks>
/// This service wraps FileSystemWatcher to detect file changes (create, modify, delete, rename)
/// in sync directories. Changes are debounced to avoid processing partial file writes.
/// Each account's sync directory is monitored independently.
/// </remarks>
public interface IFileWatcherService : IDisposable
{
    /// <summary>
    /// Starts watching a directory for file system changes.
    /// </summary>
    /// <param name="accountId">Account identifier for isolating change events.</param>
    /// <param name="localPath">Local directory path to monitor.</param>
    /// <exception cref="ArgumentNullException">Thrown if accountId or localPath is null.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown if localPath does not exist.</exception>
    void StartWatching(string accountId, string localPath);

    /// <summary>
    /// Stops watching a directory for the specified account.
    /// </summary>
    /// <param name="accountId">Account identifier whose watcher should be stopped.</param>
    void StopWatching(string accountId);

    /// <summary>
    /// Gets an observable stream of file change events from all monitored directories.
    /// </summary>
    /// <remarks>
    /// Emits debounced file change events. Changes occurring within 500ms are throttled
    /// to avoid processing incomplete file writes.
    /// </remarks>
    IObservable<FileChangeEvent> FileChanges { get; }
}
