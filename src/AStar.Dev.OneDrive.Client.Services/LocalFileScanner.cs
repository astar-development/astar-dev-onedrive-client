using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Services;

/// <inheritdoc/>
public class LocalFileScanner(ISyncRepository repo, IFileSystemAdapter fs, ILogger<LocalFileScanner> logger) : ILocalFileScanner
{

    /// <inheritdoc/>
    public async Task<(int processedCount, int newFilesCount, int modifiedFilesCount)> ScanAndSyncLocalFilesAsync(CancellationToken cancellationToken)
    {
        var localFilesList = (await fs.EnumerateFilesAsync(cancellationToken)).ToList();
        logger.LogInformation("Found {FileCount} local files to process", localFilesList.Count);
        int processedCount = 0, newFilesCount = 0, modifiedFilesCount = 0;
        foreach(LocalFileInfo localFile in localFilesList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            processedCount++;
            FileProcessResult result = await ProcessLocalFileAsync(localFile, cancellationToken);
            if(result == FileProcessResult.New)
                newFilesCount++;
            else if(result == FileProcessResult.Modified)
                modifiedFilesCount++;
        }

        return (processedCount, newFilesCount, modifiedFilesCount);
    }

    private enum FileProcessResult { None, New, Modified }

    private async Task<FileProcessResult> ProcessLocalFileAsync(LocalFileInfo localFile, CancellationToken cancellationToken)
    {
        LocalFileRecord? existingFile = await repo.GetLocalFileByPathAsync(localFile.RelativePath, cancellationToken);
        DriveItemRecord? driveItem = await repo.GetDriveItemByPathAsync(localFile.RelativePath, cancellationToken);
        if(driveItem is null)
        {
            if(existingFile is null)
            {
                await repo.AddOrUpdateLocalFileAsync(new LocalFileRecord(
                    Guid.CreateVersion7().ToString(), localFile.RelativePath, localFile.Hash, localFile.Size, localFile.LastWriteUtc, SyncState.PendingUpload), cancellationToken);
                logger.LogDebug("Marked new file for upload: {Path}", localFile.RelativePath);
                return FileProcessResult.New;
            }
            else if(existingFile.SyncState != SyncState.PendingUpload)
            {
                await repo.AddOrUpdateLocalFileAsync(existingFile with { SyncState = SyncState.PendingUpload }, cancellationToken);
                logger.LogDebug("Marked existing local file (not in OneDrive) for upload: {Path}", localFile.RelativePath);
                return FileProcessResult.New;
            }
        }
        else if(ShouldMarkAsModified(localFile, driveItem, existingFile))
        {
            if(existingFile is null)
            {
                await repo.AddOrUpdateLocalFileAsync(new LocalFileRecord(
                    driveItem.Id, localFile.RelativePath, localFile.Hash, localFile.Size, localFile.LastWriteUtc, SyncState.PendingUpload), cancellationToken);
                logger.LogDebug("Marked modified file for upload: {Path}", localFile.RelativePath);
                return FileProcessResult.Modified;
            }
            else if(ShouldUpdateExistingFileForUpload(existingFile, localFile))
            {
                await repo.AddOrUpdateLocalFileAsync(existingFile with
                {
                    Hash = localFile.Hash,
                    Size = localFile.Size,
                    LastWriteUtc = localFile.LastWriteUtc,
                    SyncState = SyncState.PendingUpload
                }, cancellationToken);
                logger.LogDebug("Marked modified file for upload: {Path}", localFile.RelativePath);
                return FileProcessResult.Modified;
            }
        }

        return FileProcessResult.None;
    }

    private static bool ShouldUpdateExistingFileForUpload(LocalFileRecord existingFile, LocalFileInfo localFile)
        => existingFile.SyncState != SyncState.PendingUpload
           || existingFile.LastWriteUtc != localFile.LastWriteUtc
           || existingFile.Size != localFile.Size
           || (localFile.Hash is not null && localFile.Hash != existingFile.Hash);

    private static bool ShouldMarkAsModified(LocalFileInfo localFile, DriveItemRecord driveItem, LocalFileRecord? existingFile) => localFile.LastWriteUtc > driveItem.LastModifiedUtc ||
            localFile.Size != driveItem.Size ||
            (localFile.Hash is not null && driveItem.Size > 0 && existingFile != null && localFile.Hash != existingFile.Hash);
}
