using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Services.Syncronisation;

public interface ISyncronisationCoordinator
{
    IObservable<SyncProgress> SyncProgress { get; }
    IObservable<SyncProgress> TransferProgress { get; }
    Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken cancellationToken);
    Task<int> GetPendingDownloadCountAsync(CancellationToken cancellationToken);
    Task<int> GetPendingUploadCountAsync(CancellationToken cancellationToken);
}
