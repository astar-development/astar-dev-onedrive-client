using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;

namespace AStar.Dev.OneDrive.Client.Services.Syncronisation;

/// <summary>
/// 
/// </summary>
/// <param name="sync"></param>
/// <param name="repo"></param>
/// <param name="transfer"></param>
public class SyncronisationCoordinator(ISyncEngine sync, ISyncRepository repo, ITransferService transfer) : ISyncronisationCoordinator
{
    /// <summary>
    ///     Gets an observable stream of sync progress updates.
    /// </summary>
    public IObservable<SyncProgress> SyncProgress => sync.Progress;

    /// <summary>
    /// Asynchronously retrieves the current delta token used to track incremental changes in the synchronization
    /// process.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the current <see cref="DeltaToken"/>
    /// if available; otherwise, <see langword="null"/>.</returns>
    public async Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken cancellationToken)
        => await repo.GetDeltaTokenAsync(cancellationToken);

    public async Task<int> GetPendingDownloadCountAsync(CancellationToken cancellationToken)
        => await repo.GetPendingDownloadCountAsync(cancellationToken);

    public async Task<int> GetPendingUploadCountAsync(CancellationToken cancellationToken)
        => await repo.GetPendingUploadCountAsync(cancellationToken);

    /// <summary>
    ///     Gets an observable stream of transfer progress updates.
    /// </summary>
    public IObservable<SyncProgress> TransferProgress => transfer.Progress;
}
