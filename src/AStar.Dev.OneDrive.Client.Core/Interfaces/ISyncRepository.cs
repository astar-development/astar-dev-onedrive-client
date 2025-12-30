using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Core.Interfaces;

public interface ISyncRepository
{
    Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken cancellationToken);
    Task SaveOrUpdateDeltaTokenAsync(DeltaToken token, CancellationToken cancellationToken);

    /// <summary>
    /// Apply a page of DriveItem metadata to the local DB.
    /// Implementations should use a transaction and batch writes.
    /// </summary>
    Task ApplyDriveItemsAsync(IEnumerable<DriveItemRecord> items, CancellationToken cancellationToken);

    Task<IEnumerable<DriveItemRecord>> GetPendingDownloadsAsync(int pageSize, int offset, CancellationToken cancellationToken);
    Task MarkLocalFileStateAsync(string driveItemId, SyncState state, CancellationToken cancellationToken);
    Task AddOrUpdateLocalFileAsync(LocalFileRecord file, CancellationToken cancellationToken);
    Task<IEnumerable<LocalFileRecord>> GetPendingUploadsAsync(int limit, CancellationToken cancellationToken);
    Task<int> GetPendingDownloadCountAsync(CancellationToken cancellationToken);
    Task<int> GetPendingUploadCountAsync(CancellationToken cancellationToken);
    Task<LocalFileRecord?> GetLocalFileByPathAsync(string relativePath, CancellationToken cancellationToken);
    Task LogTransferAsync(TransferLog log, CancellationToken cancellationToken);
    /// <summary>
    /// Gets a DriveItemRecord by its relative path, or null if not found.
    /// </summary>
    Task<DriveItemRecord?> GetDriveItemByPathAsync(string relativePath, CancellationToken cancellationToken);
    /// <summary>
    /// Gets all pending downloads (not just a page).
    /// </summary>
    Task<IEnumerable<DriveItemRecord>> GetAllPendingDownloadsAsync(CancellationToken cancellationToken);
}
