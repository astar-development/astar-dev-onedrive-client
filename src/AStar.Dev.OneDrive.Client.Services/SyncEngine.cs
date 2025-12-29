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
    /// </summary>public async Task InitialFullSyncAsync(CancellationToken ct)

    public async Task InitialFullSyncAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Starting initial full sync");
        ReportInitialSyncProgress(stopwatch);

        (var finalDelta, var pageCount, var totalItemsProcessed) = await ProcessAllDeltaPagesAsync(stopwatch, ct);

        if(!string.IsNullOrEmpty(finalDelta))
            await SaveFinalDeltaTokenAsync(finalDelta, totalItemsProcessed, stopwatch, ct);

        await ReportAndProcessTransfersAsync(pageCount, stopwatch, ct);

        stopwatch.Stop();
        logger.LogInformation("Initial full sync complete: {TotalItems} items, {Downloads} downloads, {Uploads} uploads in {ElapsedMs}ms",
            totalItemsProcessed, await repo.GetPendingDownloadCountAsync(ct), await repo.GetPendingUploadCountAsync(ct), stopwatch.ElapsedMilliseconds);
        ReportSyncCompleted(pageCount, stopwatch);
    }

    private void ReportInitialSyncProgress(Stopwatch stopwatch)
        => _progressSubject.OnNext(new SyncProgress
        {
            OperationType = SyncOperationType.Syncing,
            CurrentOperationMessage = "Starting initial full sync...",
            ProcessedFiles = 0,
            TotalFiles = 0,
            ElapsedTime = stopwatch.Elapsed
        });

    private async Task<(string? finalDelta, int pageCount, int totalItemsProcessed)> ProcessAllDeltaPagesAsync(Stopwatch stopwatch, CancellationToken ct)
    {
        string? nextOrDelta = null, finalDelta = null;
        int pageCount = 0, totalItemsProcessed = 0;
        do
        {
            DeltaPage page = await graph.GetDriveDeltaPageAsync(nextOrDelta, ct);
            await repo.ApplyDriveItemsAsync(page.Items, ct);
            totalItemsProcessed += page.Items.Count();
            nextOrDelta = page.NextLink;
            finalDelta = page.DeltaLink ?? finalDelta;
            pageCount++;
            ReportDeltaPageProgress(pageCount, totalItemsProcessed, stopwatch);
            logger.LogInformation("Applied page {PageNum}: items={Count} totalItems={Total} next={Next}",
                pageCount, page.Items.Count(), totalItemsProcessed, page.NextLink is not null);
        } while(!string.IsNullOrEmpty(nextOrDelta) && !ct.IsCancellationRequested);
        return (finalDelta, pageCount, totalItemsProcessed);
    }

    private void ReportDeltaPageProgress(int pageCount, int totalItemsProcessed, Stopwatch stopwatch)
        => _progressSubject.OnNext(new SyncProgress
        {
            OperationType = SyncOperationType.Syncing,
            CurrentOperationMessage = $"Processing delta pages (page {pageCount}, {totalItemsProcessed} items)",
            ProcessedFiles = pageCount,
            TotalFiles = 0,
            ElapsedTime = stopwatch.Elapsed
        });

    private async Task SaveFinalDeltaTokenAsync(string finalDelta, int totalItemsProcessed, Stopwatch stopwatch, CancellationToken ct)
    {
        var token = new DeltaToken(Guid.CreateVersion7().ToString(), finalDelta, DateTimeOffset.UtcNow);
        await repo.SaveOrUpdateDeltaTokenAsync(token, ct);
        logger.LogInformation("Saved delta token after processing {ItemCount} items in {ElapsedMs}ms",
            totalItemsProcessed, stopwatch.ElapsedMilliseconds);
    }

    private async Task ReportAndProcessTransfersAsync(int pageCount, Stopwatch stopwatch, CancellationToken ct)
    {
        var pendingDownloads = await repo.GetPendingDownloadCountAsync(ct);
        var pendingUploads = await repo.GetPendingUploadCountAsync(ct);
        _progressSubject.OnNext(new SyncProgress
        {
            OperationType = SyncOperationType.Syncing,
            CurrentOperationMessage = "Processing transfers...",
            ProcessedFiles = pageCount,
            TotalFiles = pageCount,
            PendingDownloads = pendingDownloads,
            PendingUploads = pendingUploads,
            ElapsedTime = stopwatch.Elapsed
        });
        await transfer.ProcessPendingDownloadsAsync(ct);
        await transfer.ProcessPendingUploadsAsync(ct);
    }

    private void ReportSyncCompleted(int pageCount, Stopwatch stopwatch)
        => _progressSubject.OnNext(new SyncProgress
        {
            OperationType = SyncOperationType.Completed,
            CurrentOperationMessage = "Initial sync completed",
            ProcessedFiles = pageCount,
            TotalFiles = pageCount,
            PendingDownloads = 0,
            PendingUploads = 0,
            ElapsedTime = stopwatch.Elapsed
        });

    public async Task IncrementalSyncAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Starting incremental sync");
        ReportIncrementalSyncStart(stopwatch);

        DeltaToken token = await repo.GetDeltaTokenAsync(ct) ?? throw new InvalidOperationException("Delta token missing; run initial sync first.");
        DeltaPage page = await graph.GetDriveDeltaPageAsync(token.Token, ct);
        await repo.ApplyDriveItemsAsync(page.Items, ct);

        if(!string.IsNullOrEmpty(page.DeltaLink))
            await repo.SaveOrUpdateDeltaTokenAsync(token with { Token = page.DeltaLink, LastSyncedUtc = DateTimeOffset.UtcNow }, ct);

        await ReportAndProcessTransfersAsync(1, stopwatch, ct);

        stopwatch.Stop();
        logger.LogInformation("Incremental sync complete: {ItemCount} items, {Downloads} downloads, {Uploads} uploads in {ElapsedMs}ms",
            page.Items.Count(), await repo.GetPendingDownloadCountAsync(ct), await repo.GetPendingUploadCountAsync(ct), stopwatch.ElapsedMilliseconds);
        ReportIncrementalSyncCompleted(stopwatch);
    }

    private void ReportIncrementalSyncStart(Stopwatch stopwatch)
        => _progressSubject.OnNext(new SyncProgress
        {
            OperationType = SyncOperationType.Syncing,
            CurrentOperationMessage = "Starting incremental sync...",
            ProcessedFiles = 0,
            TotalFiles = 0,
            ElapsedTime = stopwatch.Elapsed
        });

    private void ReportIncrementalSyncCompleted(Stopwatch stopwatch)
        => _progressSubject.OnNext(new SyncProgress
        {
            OperationType = SyncOperationType.Completed,
            CurrentOperationMessage = "Incremental sync completed",
            ProcessedFiles = 1,
            TotalFiles = 1,
            PendingDownloads = 0,
            PendingUploads = 0,
            ElapsedTime = stopwatch.Elapsed
        });

    public async Task ScanLocalFilesAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Starting local file sync");
        ReportLocalScanStart(stopwatch);

        await Task.Run(async () =>
        {
            var localFilesList = (await fs.EnumerateFilesAsync(ct)).ToList();
            logger.LogInformation("Found {FileCount} local files to process", localFilesList.Count);
            ReportLocalScanProgress(0, localFilesList.Count, stopwatch);

            (var processedCount, var newFilesCount, var modifiedFilesCount) = await ProcessLocalFilesAsync(localFilesList, ct, stopwatch);

            var pendingUploads = await repo.GetPendingUploadCountAsync(ct);
            stopwatch.Stop();
            logger.LogInformation("Local file sync complete: {Total} files scanned, {New} new, {Modified} modified, {Pending} pending uploads in {ElapsedMs}ms",
                processedCount, newFilesCount, modifiedFilesCount, pendingUploads, stopwatch.ElapsedMilliseconds);

            ReportLocalScanCompleted(processedCount, pendingUploads, stopwatch);
        }, ct);
    }

    private void ReportLocalScanStart(Stopwatch stopwatch)
        => _progressSubject.OnNext(new SyncProgress
        {
            OperationType = SyncOperationType.Syncing,
            CurrentOperationMessage = "Scanning local files to sync...",
            ProcessedFiles = 0,
            TotalFiles = 0,
            ElapsedTime = stopwatch.Elapsed
        });

    private void ReportLocalScanProgress(int processed, int total, Stopwatch stopwatch)
        => _progressSubject.OnNext(new SyncProgress
        {
            OperationType = SyncOperationType.Syncing,
            CurrentOperationMessage = $"Scanning local files...",
            ProcessedFiles = processed,
            TotalFiles = total,
            ElapsedTime = stopwatch.Elapsed
        });

    private void ReportLocalScanStep(int processed, int total, Stopwatch stopwatch)
        => _progressSubject.OnNext(new SyncProgress
        {
            OperationType = SyncOperationType.Syncing,
            CurrentOperationMessage = $"Scanning local files ({processed}/{total})...",
            ProcessedFiles = processed,
            TotalFiles = total,
            ElapsedTime = stopwatch.Elapsed
        });

    private void ReportLocalScanCompleted(int processed, int pendingUploads, Stopwatch stopwatch)
        => _progressSubject.OnNext(new SyncProgress
        {
            OperationType = SyncOperationType.Completed,
            CurrentOperationMessage = "Local file sync completed",
            ProcessedFiles = processed,
            TotalFiles = processed,
            PendingUploads = pendingUploads,
            ElapsedTime = stopwatch.Elapsed
        });

    private async Task<(int processedCount, int newFilesCount, int modifiedFilesCount)> ProcessLocalFilesAsync(
        List<LocalFileInfo> localFilesList, CancellationToken ct, Stopwatch stopwatch)
    {
        int processedCount = 0, newFilesCount = 0, modifiedFilesCount = 0;
        foreach(LocalFileInfo localFile in localFilesList)
        {
            ct.ThrowIfCancellationRequested();
            processedCount++;
            if(processedCount % 50 == 0 || processedCount == localFilesList.Count)
                ReportLocalScanStep(processedCount, localFilesList.Count, stopwatch);

            FileProcessResult result = await ProcessLocalFileAsync(localFile, ct);
            if(result == FileProcessResult.New)
                newFilesCount++;
            else if(result == FileProcessResult.Modified)
                modifiedFilesCount++;
        }

        return (processedCount, newFilesCount, modifiedFilesCount);
    }

    private enum FileProcessResult { None, New, Modified }

    private async Task<FileProcessResult> ProcessLocalFileAsync(LocalFileInfo localFile, CancellationToken ct)
    {
        LocalFileRecord? existingFile = await repo.GetLocalFileByPathAsync(localFile.RelativePath, ct);
        DriveItemRecord? driveItem = await repo.GetDriveItemByPathAsync(localFile.RelativePath, ct);

        if(driveItem is null)
        {
            if(existingFile is null)
            {
                await repo.AddOrUpdateLocalFileAsync(new LocalFileRecord(
                    Guid.CreateVersion7().ToString(), localFile.RelativePath, localFile.Hash, localFile.Size, localFile.LastWriteUtc, SyncState.PendingUpload), ct);
                logger.LogDebug("Marked new file for upload: {Path}", localFile.RelativePath);
                return FileProcessResult.New;
            }
            else if(existingFile.SyncState != SyncState.PendingUpload)
            {
                await repo.AddOrUpdateLocalFileAsync(existingFile with { SyncState = SyncState.PendingUpload }, ct);
                logger.LogDebug("Marked existing local file (not in OneDrive) for upload: {Path}", localFile.RelativePath);
                return FileProcessResult.New;
            }
        }
        else if(ShouldMarkAsModified(localFile, driveItem, existingFile))
        {
            if(existingFile is null)
            {
                await repo.AddOrUpdateLocalFileAsync(new LocalFileRecord(
                    driveItem.Id, localFile.RelativePath, localFile.Hash, localFile.Size, localFile.LastWriteUtc, SyncState.PendingUpload), ct);
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
                }, ct);
                logger.LogDebug("Marked modified file for upload: {Path}", localFile.RelativePath);
                return FileProcessResult.Modified;
            }
        }

        return FileProcessResult.None;
    }
    /// <summary>
    /// Determines if an existing local file record should be updated for upload based on sync state, timestamp, size, or hash.
    /// </summary>
    private static bool ShouldUpdateExistingFileForUpload(LocalFileRecord existingFile, LocalFileInfo localFile)
        => existingFile.SyncState != SyncState.PendingUpload
           || existingFile.LastWriteUtc != localFile.LastWriteUtc
           || existingFile.Size != localFile.Size
           || (localFile.Hash is not null && localFile.Hash != existingFile.Hash);

    private static bool ShouldMarkAsModified(LocalFileInfo localFile, DriveItemRecord driveItem, LocalFileRecord? existingFile) => localFile.LastWriteUtc > driveItem.LastModifiedUtc ||
            localFile.Size != driveItem.Size ||
            (localFile.Hash is not null && driveItem.Size > 0 && existingFile != null && localFile.Hash != existingFile.Hash);

    public async Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken ct) => await repo.GetDeltaTokenAsync(ct);
}
