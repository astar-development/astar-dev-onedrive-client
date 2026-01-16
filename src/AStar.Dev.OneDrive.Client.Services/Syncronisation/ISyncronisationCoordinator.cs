using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Services.Syncronisation;

public interface ISyncronisationCoordinator
{
    IObservable<SyncProgress> SyncProgress { get; }
    IObservable<SyncProgress> TransferProgress { get; }
    Task<DeltaToken?> GetDeltaTokenAsync(string accountId, CancellationToken cancellationToken);
    Task<int> GetPendingDownloadCountAsync(string accountId, CancellationToken cancellationToken);
    Task<int> GetPendingUploadCountAsync(string accountId, CancellationToken cancellationToken);
}
