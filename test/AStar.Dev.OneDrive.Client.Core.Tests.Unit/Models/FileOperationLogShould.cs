using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Client.Core.Tests.Unit.Models;

public class FileOperationLogShould
{
    [Fact]
    public void CreateSyncConflictLogCorrectly()
    {
        var accountId = "account-id";
        var sessionId = "session-id";
        var filePath = "/path/to/file.txt";
        var localPath = "/onedrive/path/to/file.txt";
        var oneDriveId = "onedrive-item-id";
        FileOperation operationType = FileOperation.ConflictDetected;
        var localHash = "local-file-hash";
        var fileSize = 2048L;
        DateTime lastModifiedUtc = DateTime.UtcNow.AddSeconds(-30);
        DateTime remoteLastModifiedUtc = DateTime.UtcNow;
        var reason = $"Conflict: Both local and remote changed. Local modified: {lastModifiedUtc:yyyy-MM-dd HH:mm:ss}, Remote modified: {remoteLastModifiedUtc:yyyy-MM-dd HH:mm:ss}";

        var fileOperationLog = FileOperationLog.CreateSyncConflictLog(
            sessionId,
            accountId,
            filePath,
            localPath,
            oneDriveId,
            localHash,
            fileSize,
            lastModifiedUtc,
            remoteLastModifiedUtc);

        _ = fileOperationLog.Id.ShouldNotBeNull();
        fileOperationLog.AccountId.ShouldBe(accountId);
        fileOperationLog.SyncSessionId.ShouldBe(sessionId);
        fileOperationLog.FilePath.ShouldBe(filePath);
        fileOperationLog.Operation.ShouldBe(operationType);
        fileOperationLog.FilePath.ShouldBe(filePath);
        fileOperationLog.FileSize.ShouldBe(fileSize);
        fileOperationLog.LastModifiedUtc.ShouldBe(lastModifiedUtc);
        fileOperationLog.Reason.ShouldBe(reason);
    }

    [Fact]
    public void CreateDownloadLogCorrectly()
    {
        var accountId = "account-id";
        var sessionId = "session-id";
        var filePath = "/path/to/file.txt";
        var localPath = "/onedrive/path/to/file.txt";
        var oneDriveId = "onedrive-item-id";
        FileOperation operationType = FileOperation.Download;
        var localHash = "local-file-hash";
        var fileSize = 2048L;
        DateTime lastModifiedUtc = DateTime.UtcNow.AddSeconds(-30);
        DateTime remoteLastModifiedUtc = DateTime.UtcNow;
        var reason = $"Conflict: Both local and remote changed. Local modified: {lastModifiedUtc:yyyy-MM-dd HH:mm:ss}, Remote modified: {remoteLastModifiedUtc:yyyy-MM-dd HH:mm:ss}";

        var fileOperationLog = FileOperationLog.CreateDownloadLog(
            sessionId,
            accountId,
            filePath,
            localPath,
            oneDriveId,
            localHash,
            fileSize,
            lastModifiedUtc,
            reason);

        _ = fileOperationLog.Id.ShouldNotBeNull();
        fileOperationLog.AccountId.ShouldBe(accountId);
        fileOperationLog.SyncSessionId.ShouldBe(sessionId);
        fileOperationLog.FilePath.ShouldBe(filePath);
        fileOperationLog.Operation.ShouldBe(operationType);
        fileOperationLog.FilePath.ShouldBe(filePath);
        fileOperationLog.FileSize.ShouldBe(fileSize);
        fileOperationLog.LastModifiedUtc.ShouldBe(lastModifiedUtc);
        fileOperationLog.Reason.ShouldBe(reason);
    }

    [Fact]
    public void CreateUploadLogCorrectly()
    {
        var accountId = "account-id";
        var sessionId = "session-id";
        var filePath = "/path/to/file.txt";
        var localPath = "/onedrive/path/to/file.txt";
        var oneDriveId = "onedrive-item-id";
        FileOperation operationType = FileOperation.Upload;
        var localHash = "local-file-hash";
        var fileSize = 2048L;
        DateTime lastModifiedUtc = DateTime.UtcNow.AddSeconds(-30);
        DateTime remoteLastModifiedUtc = DateTime.UtcNow;
        var reason = $"Conflict: Both local and remote changed. Local modified: {lastModifiedUtc:yyyy-MM-dd HH:mm:ss}, Remote modified: {remoteLastModifiedUtc:yyyy-MM-dd HH:mm:ss}";

        var fileOperationLog = FileOperationLog.CreateUploadLog(
            sessionId,
            accountId,
            filePath,
            localPath,
            oneDriveId,
            localHash,
            fileSize,
            lastModifiedUtc,
            reason);

        _ = fileOperationLog.Id.ShouldNotBeNull();
        fileOperationLog.AccountId.ShouldBe(accountId);
        fileOperationLog.SyncSessionId.ShouldBe(sessionId);
        fileOperationLog.FilePath.ShouldBe(filePath);
        fileOperationLog.Operation.ShouldBe(operationType);
        fileOperationLog.FilePath.ShouldBe(filePath);
        fileOperationLog.FileSize.ShouldBe(fileSize);
        fileOperationLog.LastModifiedUtc.ShouldBe(lastModifiedUtc);
        fileOperationLog.Reason.ShouldBe(reason);
    }
}
