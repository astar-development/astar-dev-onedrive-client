namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Service for synchronizing files between local storage and OneDrive.
/// </summary>
public interface ISyncEngine
{
    /// <summary>
    /// Gets an observable stream of sync progress updates.
    /// </summary>
    IObservable<Core.Entities.SyncState> Progress { get; }

    /// <summary>
    /// Starts synchronization for the specified account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartSyncAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops any ongoing synchronization.
    /// </summary>
    Task StopSyncAsync();

    /// <summary>
    /// Gets all detected conflicts for the specified account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of unresolved conflicts.</returns>
    Task<IReadOnlyList<Core.Entities.SyncConflict>> GetConflictsAsync(string accountId, CancellationToken cancellationToken = default);
}
