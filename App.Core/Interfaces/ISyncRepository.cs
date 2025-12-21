using App.Core.Entities;

namespace App.Core.Interfaces;

public interface ISyncRepository
{
    Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken ct);
    Task SaveOrUpdateDeltaTokenAsync(DeltaToken token, CancellationToken ct);

    /// <summary>
    /// Apply a page of DriveItem metadata to the local DB.
    /// Implementations should use a transaction and batch writes.
    /// </summary>
    Task ApplyDriveItemsAsync(IEnumerable<DriveItemRecord> items, CancellationToken ct);

    Task<IEnumerable<DriveItemRecord>> GetPendingDownloadsAsync(int limit, CancellationToken ct);
    Task MarkLocalFileStateAsync(string driveItemId, SyncState state, CancellationToken ct);
    Task AddOrUpdateLocalFileAsync(LocalFileRecord file, CancellationToken ct);
    Task<IEnumerable<LocalFileRecord>> GetPendingUploadsAsync(int limit, CancellationToken ct);

    Task LogTransferAsync(TransferLog log, CancellationToken ct);
}
