using AStar.Dev.OneDrive.Client.FromV3.Models.Enums;

namespace AStar.Dev.OneDrive.Client.FromV3.Models;

/// <summary>
/// Represents a detailed log entry for a file operation during sync.
/// </summary>
/// <param name="Id">Unique identifier for the log entry.</param>
/// <param name="SyncSessionId">The sync session this operation belongs to.</param>
/// <param name="AccountId">The account identifier.</param>
/// <param name="Timestamp">When the operation occurred.</param>
/// <param name="Operation">Type of operation performed.</param>
/// <param name="FilePath">OneDrive path of the file.</param>
/// <param name="LocalPath">Local file system path.</param>
/// <param name="OneDriveId">OneDrive item ID (if available).</param>
/// <param name="FileSize">Size of the file in bytes.</param>
/// <param name="LocalHash">SHA256 hash of the local file.</param>
/// <param name="RemoteHash">SHA256 hash from OneDrive (if available).</param>
/// <param name="LastModifiedUtc">Last modified timestamp.</param>
/// <param name="Reason">Human-readable reason for the operation.</param>
public record FileOperationLog(
    string Id, string SyncSessionId, string AccountId, DateTime Timestamp, FileOperation Operation, string FilePath, string LocalPath,
    string? OneDriveId, long FileSize, string? LocalHash, string? RemoteHash, DateTime LastModifiedUtc, string Reason)
{
    public static FileOperationLog CreateSyncConflictLog(string syncSessionId, string accountId, string filePath, string localPath,
        string oneDriveId, string? localHash, long fileSize, DateTime lastModifiedUtc, DateTime remoteFileLastModifiedUtc) => new(
            Id: Guid.NewGuid().ToString(), SyncSessionId: syncSessionId, AccountId: accountId, Timestamp: DateTime.UtcNow,
            Operation: FileOperation.ConflictDetected, FilePath: filePath, LocalPath: localPath, OneDriveId: oneDriveId,
            FileSize: fileSize, LocalHash: localHash, RemoteHash: null, LastModifiedUtc: lastModifiedUtc,
            Reason: $"Conflict: Both local and remote changed. Local modified: {lastModifiedUtc:yyyy-MM-dd HH:mm:ss}, Remote modified: {remoteFileLastModifiedUtc:yyyy-MM-dd HH:mm:ss}");

    public static FileOperationLog CreateDownloadLog(string syncSessionId, string accountId, string filePath, string localPath,
        string? oneDriveId, string? localHash, long fileSize, DateTime lastModifiedUtc, string reason) => new(
            Id: Guid.NewGuid().ToString(), SyncSessionId: syncSessionId, AccountId: accountId, Timestamp: DateTime.UtcNow,
            Operation: FileOperation.Download, FilePath: filePath, LocalPath: localPath, OneDriveId: oneDriveId,
            FileSize: fileSize, LocalHash: localHash, RemoteHash: null, LastModifiedUtc: lastModifiedUtc,
            Reason: reason);

    public static FileOperationLog CreateUploadLog(string syncSessionId, string accountId, string filePath, string localPath,
        string? oneDriveId, string? localHash, long fileSize, DateTime lastModifiedUtc, string reason) => new(
            Id: Guid.NewGuid().ToString(), SyncSessionId: syncSessionId, AccountId: accountId, Timestamp: DateTime.UtcNow,
            Operation: FileOperation.Upload, FilePath: filePath, LocalPath: localPath, OneDriveId: oneDriveId,
            FileSize: fileSize, LocalHash: localHash, RemoteHash: null, LastModifiedUtc: lastModifiedUtc,
            Reason: reason);
}
