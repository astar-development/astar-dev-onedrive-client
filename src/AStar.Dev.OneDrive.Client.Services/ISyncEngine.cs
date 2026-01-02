using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Services;

/// <summary>
///     Defines the contract for synchronizing OneDrive items with the local repository.
/// </summary>
public interface ISyncEngine
{
    /// <summary>
    ///     Gets an observable stream of sync progress updates.
    /// </summary>
    IObservable<SyncProgress> Progress { get; }

    /// <summary>
    ///     Performs the initial full enumeration using Graph delta. Pages until exhausted,
    ///     persists DriveItemRecords and the final deltaLink for incremental syncs.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitialFullSyncAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Performs an incremental sync using the stored delta token.
    /// </summary>
    /// <param name="deltaToken">The delta token to use for the sync.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task IncrementalSyncAsync(DeltaToken deltaToken, CancellationToken cancellationToken);

    /// <summary>
    ///     Scans the local file system and marks new or modified files for upload.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ScanLocalFilesAsync(CancellationToken cancellationToken);

    Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken cancellationToken);
}
