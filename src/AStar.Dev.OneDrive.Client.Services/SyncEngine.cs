using System.Diagnostics;
using System.Reactive.Subjects;
using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Services;

public sealed class SyncEngine(ISyncRepository repo, IGraphClient graph, ITransferService transfer, ILogger<SyncEngine> logger) : ISyncEngine
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
}
