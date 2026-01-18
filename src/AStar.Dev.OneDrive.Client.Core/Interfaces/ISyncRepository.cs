using AStar.Dev.OneDrive.Client.Core.Entities;
using SyncState = AStar.Dev.OneDrive.Client.Core.Entities.Enums.SyncState;

namespace AStar.Dev.OneDrive.Client.Core.Interfaces;

public interface ISyncRepository
{
    Task<DeltaToken?> GetDeltaTokenAsync(string accountId, CancellationToken cancellationToken);
    Task SaveOrUpdateDeltaTokenAsync(string accountId, DeltaToken token, CancellationToken cancellationToken);

    /// <summary>
    /// Apply a page of DriveItem metadata to the local DB.
    /// Implementations should use a transaction and batch writes.
    /// </summary>
    Task ApplyDriveItemsAsync(string accountId, IEnumerable<DriveItemRecord> items, CancellationToken cancellationToken);

    Task<IEnumerable<DriveItemRecord>> GetPendingDownloadsAsync(string accountId, int pageSize, int offset, CancellationToken cancellationToken);
    Task MarkLocalFileStateAsync(string accountId, string driveItemId, SyncState state, CancellationToken cancellationToken);
    Task AddOrUpdateLocalFileAsync(string accountId, LocalFileRecord file, CancellationToken cancellationToken);
    Task<IEnumerable<LocalFileRecord>> GetPendingUploadsAsync(string accountId, int limit, CancellationToken cancellationToken);
    Task<int> GetPendingDownloadCountAsync(string accountId, CancellationToken cancellationToken);
    Task<int> GetPendingUploadCountAsync(string accountId, CancellationToken cancellationToken);
    Task<LocalFileRecord?> GetLocalFileByPathAsync(string accountId, string relativePath, CancellationToken cancellationToken);
    Task LogTransferAsync(string accountId, TransferLog log, CancellationToken cancellationToken);
    /// <summary>
    /// Gets a DriveItemRecord by its relative path, or null if not found.
    /// </summary>
    Task<DriveItemRecord?> GetDriveItemByPathAsync(string accountId, string relativePath, CancellationToken cancellationToken);
    /// <summary>
    /// Gets all pending downloads (not just a page).
    /// </summary>
    Task<IEnumerable<DriveItemRecord>> GetAllPendingDownloadsAsync(string accountId, CancellationToken cancellationToken);
}
