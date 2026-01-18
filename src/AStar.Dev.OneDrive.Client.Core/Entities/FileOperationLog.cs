using AStar.Dev.OneDrive.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Client.Core.Entities;

/// <summary>
/// Database entity for file operation log.
/// </summary>
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
