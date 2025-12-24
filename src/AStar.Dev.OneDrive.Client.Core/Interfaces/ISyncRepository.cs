using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Core.Interfaces;

public interface ISyncRepository
{
    Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken ct);
    Task SaveOrUpdateDeltaTokenAsync(DeltaToken token, CancellationToken ct);

    /// <summary>
    /// Apply a page of DriveItem metadata to the local DB.
    /// Implementations should use a transaction and batch writes.
    /// </summary>
    Task ApplyDriveItemsAsync(IEnumerable<DriveItemRecord> items, CancellationToken ct);

    Task<IEnumerable<DriveItemRecord>> GetPendingDownloadsAsync(int pageSize, int offset, CancellationToken ct);
    Task MarkLocalFileStateAsync(string driveItemId, SyncState state, CancellationToken ct);
    Task AddOrUpdateLocalFileAsync(LocalFileRecord file, CancellationToken ct);
    Task<IEnumerable<LocalFileRecord>> GetPendingUploadsAsync(int limit, CancellationToken ct);
    Task<int> GetPendingDownloadCountAsync(CancellationToken ct);
    Task<int> GetPendingUploadCountAsync(CancellationToken ct);
    Task<LocalFileRecord?> GetLocalFileByPathAsync(string relativePath, CancellationToken ct);
    Task LogTransferAsync(TransferLog log, CancellationToken ct);
    }
