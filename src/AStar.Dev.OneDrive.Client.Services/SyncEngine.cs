using System.Diagnostics;
using System.Reactive.Subjects;
using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Services;

public sealed class SyncEngine(ISyncRepository repo, IGraphClient graph, ITransferService transfer, IFileSystemAdapter fs, ILogger<SyncEngine> logger) : ISyncEngine
{
    private readonly Subject<SyncProgress> _progressSubject = new();

    public IObservable<SyncProgress> Progress => _progressSubject;

    /// <summary>
    /// Performs the initial full enumeration using Graph delta. Pages until exhausted,
    /// persists DriveItemRecords and the final deltaLink for incremental syncs.
    /// </summary>
    public async Task InitialFullSyncAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Starting initial full sync");
        _progressSubject.OnNext(new SyncProgress
        {
            CurrentOperation = "Starting initial full sync...",
            ProcessedFiles = 0,
            TotalFiles = 0,
            ElapsedTime = stopwatch.Elapsed
        });

        string? nextOrDelta = null;
        string? finalDelta = null;
        var pageCount = 0;
        var totalItemsProcessed = 0;

        do
        {
            DeltaPage page = await graph.GetDriveDeltaPageAsync(nextOrDelta, ct);
            await repo.ApplyDriveItemsAsync(page.Items, ct);
            totalItemsProcessed += page.Items.Count();
            nextOrDelta = page.NextLink;
            finalDelta = page.DeltaLink ?? finalDelta;
            pageCount++;

            _progressSubject.OnNext(new SyncProgress
            {
                CurrentOperation = $"Processing delta pages (page {pageCount}, {totalItemsProcessed} items)",
                ProcessedFiles = pageCount,
                TotalFiles = 0,
                ElapsedTime = stopwatch.Elapsed
            });

            logger.LogInformation("Applied page {PageNum}: items={Count} totalItems={Total} next={Next}",
                pageCount, page.Items.Count(), totalItemsProcessed, page.NextLink is not null);
        } while(!string.IsNullOrEmpty(nextOrDelta) && !ct.IsCancellationRequested);

        if(!string.IsNullOrEmpty(finalDelta))
        {
            var token = new DeltaToken(Guid.NewGuid().ToString(), finalDelta, DateTimeOffset.UtcNow);
            await repo.SaveOrUpdateDeltaTokenAsync(token, ct);
            logger.LogInformation("Saved delta token after processing {ItemCount} items in {ElapsedMs}ms",
                totalItemsProcessed, stopwatch.ElapsedMilliseconds);
        }

        // Get actual counts from repository
        var pendingDownloads = await repo.GetPendingDownloadCountAsync(ct);
        var pendingUploads = await repo.GetPendingUploadCountAsync(ct);

        _progressSubject.OnNext(new SyncProgress
        {
            CurrentOperation = "Processing transfers...",
            ProcessedFiles = pageCount,
            TotalFiles = pageCount,
            PendingDownloads = pendingDownloads,
            PendingUploads = pendingUploads,
            ElapsedTime = stopwatch.Elapsed
        });

        // Kick off transfers after DB is updated
        await transfer.ProcessPendingDownloadsAsync(ct);
        await transfer.ProcessPendingUploadsAsync(ct);

        stopwatch.Stop();
        logger.LogInformation("Initial full sync complete: {TotalItems} items, {Downloads} downloads, {Uploads} uploads in {ElapsedMs}ms",
            totalItemsProcessed, pendingDownloads, pendingUploads, stopwatch.ElapsedMilliseconds);
        _progressSubject.OnNext(new SyncProgress
        {
            CurrentOperation = "Initial sync completed",
            ProcessedFiles = pageCount,
            TotalFiles = pageCount,
            PendingDownloads = 0,
            PendingUploads = 0,
            ElapsedTime = stopwatch.Elapsed
        });
    }

    /// <summary>
    /// Performs an incremental sync using the stored delta token.
    /// </summary>
    public async Task IncrementalSyncAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Starting incremental sync");
        _progressSubject.OnNext(new SyncProgress
        {
            CurrentOperation = "Starting incremental sync...",
            ProcessedFiles = 0,
            TotalFiles = 0,
            ElapsedTime = stopwatch.Elapsed
        });

        DeltaToken token = await repo.GetDeltaTokenAsync(ct) ?? throw new InvalidOperationException("Delta token missing; run initial sync first.");
        DeltaPage page = await graph.GetDriveDeltaPageAsync(token.Token, ct);
        var itemCount = page.Items.Count();
        await repo.ApplyDriveItemsAsync(page.Items, ct);

        if(!string.IsNullOrEmpty(page.DeltaLink))
        {
            await repo.SaveOrUpdateDeltaTokenAsync(token with { Token = page.DeltaLink, LastSyncedUtc = DateTimeOffset.UtcNow }, ct);
            logger.LogInformation("Updated delta token after processing {ItemCount} items in {ElapsedMs}ms",
                itemCount, stopwatch.ElapsedMilliseconds);
        }

        // Get actual counts from repository
        var pendingDownloads = await repo.GetPendingDownloadCountAsync(ct);
        var pendingUploads = await repo.GetPendingUploadCountAsync(ct);

        _progressSubject.OnNext(new SyncProgress
        {
            CurrentOperation = "Processing transfers...",
            ProcessedFiles = 1,
            TotalFiles = 1,
            PendingDownloads = pendingDownloads,
            PendingUploads = pendingUploads,
            ElapsedTime = stopwatch.Elapsed
        });

        await transfer.ProcessPendingDownloadsAsync(ct);
        await transfer.ProcessPendingUploadsAsync(ct);

        stopwatch.Stop();
        logger.LogInformation("Incremental sync complete: {ItemCount} items, {Downloads} downloads, {Uploads} uploads in {ElapsedMs}ms",
            itemCount, pendingDownloads, pendingUploads, stopwatch.ElapsedMilliseconds);
        _progressSubject.OnNext(new SyncProgress
        {
            CurrentOperation = "Incremental sync completed",
            ProcessedFiles = 1,
            TotalFiles = 1,
            PendingDownloads = 0,
            PendingUploads = 0,
            ElapsedTime = stopwatch.Elapsed
        });
    }

    /// <summary>
    /// Scans the local file system and marks new or modified files for upload.
    /// </summary>
    public async Task ScanLocalFilesAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Starting local file scan");
        _progressSubject.OnNext(new SyncProgress
        {
            CurrentOperation = "Scanning local files...",
            ProcessedFiles = 0,
            TotalFiles = 0,
            ElapsedTime = stopwatch.Elapsed
        });

        IEnumerable<LocalFileInfo> localFiles = await fs.EnumerateFilesAsync(ct);
        var localFilesList = localFiles.ToList();
        var processedCount = 0;
        var newFilesCount = 0;
        var modifiedFilesCount = 0;

        logger.LogInformation("Found {FileCount} local files to process", localFilesList.Count);

        _progressSubject.OnNext(new SyncProgress
        {
            CurrentOperation = $"Scanning local files...",
            ProcessedFiles = 0,
            TotalFiles = localFilesList.Count,
            ElapsedTime = stopwatch.Elapsed
        });

        foreach(LocalFileInfo? localFile in localFilesList)
        {
            ct.ThrowIfCancellationRequested();

            processedCount++;

            if(processedCount % 50 == 0 || processedCount == localFilesList.Count)
            {
                _progressSubject.OnNext(new SyncProgress
                {
                    CurrentOperation = $"Scanning local files ({processedCount}/{localFilesList.Count})...",
                    ProcessedFiles = processedCount,
                    TotalFiles = localFilesList.Count,
                    ElapsedTime = stopwatch.Elapsed
                });
            }

            LocalFileRecord? existingFile = await repo.GetLocalFileByPathAsync(localFile.RelativePath, ct);
            DriveItemRecord? driveItem = await repo.GetDriveItemByPathAsync(localFile.RelativePath, ct);

            // Only mark as PendingUpload if:
            // 1. Not present in OneDrive (driveItem == null)
            // 2. Present in OneDrive, but local file is newer or different
            if(driveItem is null)
            {
                // New local file, not in OneDrive
                if(existingFile is null)
                {
                    var newFile = new LocalFileRecord(
                                    Guid.NewGuid().ToString(),
                                    localFile.RelativePath,
                                    localFile.Hash,
                                    localFile.Size,
                                    localFile.LastWriteUtc,
                                    SyncState.PendingUpload
                                );
                    await repo.AddOrUpdateLocalFileAsync(newFile, ct);
                    newFilesCount++;
                    logger.LogDebug("Marked new file for upload: {Path}", localFile.RelativePath);
                }
                else if(existingFile.SyncState != SyncState.PendingUpload)
                {
                    LocalFileRecord updatedFile = existingFile with { SyncState = SyncState.PendingUpload };
                    await repo.AddOrUpdateLocalFileAsync(updatedFile, ct);
                    newFilesCount++;
                    logger.LogDebug("Marked existing local file (not in OneDrive) for upload: {Path}", localFile.RelativePath);
                }
            }
            else
            {
                // File exists in OneDrive, only mark as upload if local file is newer/different
                var isModified =
                                localFile.LastWriteUtc > driveItem.LastModifiedUtc ||
                                localFile.Size != driveItem.Size ||
                                (localFile.Hash is not null && driveItem.Size > 0 && existingFile != null && localFile.Hash != existingFile.Hash);

                if(isModified)
                {
                    if(existingFile is null)
                    {
                        var newFile = new LocalFileRecord(
                                        driveItem.Id,
                                        localFile.RelativePath,
                                        localFile.Hash,
                                        localFile.Size,
                                        localFile.LastWriteUtc,
                                        SyncState.PendingUpload
                                    );
                        await repo.AddOrUpdateLocalFileAsync(newFile, ct);
                        modifiedFilesCount++;
                        logger.LogDebug("Marked modified file for upload: {Path}", localFile.RelativePath);
                    }
                    else if(existingFile.SyncState != SyncState.PendingUpload ||
                             existingFile.LastWriteUtc != localFile.LastWriteUtc ||
                             existingFile.Size != localFile.Size ||
                             (localFile.Hash is not null && localFile.Hash != existingFile.Hash))
                    {
                        LocalFileRecord updatedFile = existingFile with
                        {
                            Hash = localFile.Hash,
                            Size = localFile.Size,
                            LastWriteUtc = localFile.LastWriteUtc,
                            SyncState = SyncState.PendingUpload
                        };
                        await repo.AddOrUpdateLocalFileAsync(updatedFile, ct);
                        modifiedFilesCount++;
                        logger.LogDebug("Marked modified file for upload: {Path}", localFile.RelativePath);
                    }
                }
            }
        }

        var pendingUploads = await repo.GetPendingUploadCountAsync(ct);

        stopwatch.Stop();
        logger.LogInformation("Local file scan complete: {Total} files scanned, {New} new, {Modified} modified, {Pending} pending uploads in {ElapsedMs}ms",
            processedCount, newFilesCount, modifiedFilesCount, pendingUploads, stopwatch.ElapsedMilliseconds);

        _progressSubject.OnNext(new SyncProgress
        {
            CurrentOperation = "Local file scan completed",
            ProcessedFiles = processedCount,
            TotalFiles = processedCount,
            PendingUploads = pendingUploads,
            ElapsedTime = stopwatch.Elapsed
        });
    }
}
