namespace AStar.Dev.OneDrive.Client.Services;

/// <summary>
///     Defines the contract for managing file transfers between OneDrive and local storage.
/// </summary>
public interface ITransferService
{
    /// <summary>
    ///     Gets an observable stream of transfer progress updates.
    /// </summary>
    IObservable<SyncProgress> Progress { get; }

    /// <summary>
    ///     Pulls pending downloads from repository in batches and downloads them with bounded concurrency.
    /// </summary>
    /// <param name="ct">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessPendingDownloadsAsync(CancellationToken ct);

    /// <summary>
    ///     Scans repository for pending uploads and uploads them using upload sessions and chunked uploads.
    /// </summary>
    /// <param name="ct">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessPendingUploadsAsync(CancellationToken ct);
}
