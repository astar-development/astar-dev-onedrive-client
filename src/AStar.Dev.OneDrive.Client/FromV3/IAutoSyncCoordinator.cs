namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Coordinates automatic synchronization based on file system changes.
/// </summary>
/// <remarks>
/// This service monitors local file system changes using <see cref="IFileWatcherService"/>
/// and automatically triggers synchronization operations when changes are detected.
/// </remarks>
public interface IAutoSyncCoordinator : IDisposable
{
    /// <summary>
    /// Starts monitoring a local directory for changes and triggers sync when changes are detected.
    /// </summary>
    /// <param name="accountId">The unique identifier of the account.</param>
    /// <param name="localPath">The local directory path to monitor.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartMonitoringAsync(string accountId, string localPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops monitoring file changes for the specified account.
    /// </summary>
    /// <param name="accountId">The unique identifier of the account.</param>
    void StopMonitoring(string accountId);
}
