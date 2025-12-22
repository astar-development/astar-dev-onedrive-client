using System.Reactive.Subjects;
using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Models;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Services;

public sealed class SyncEngine(ISyncRepository repo, IGraphClient graph, TransferService transfer, ILogger<SyncEngine> logger)
{
    private readonly Subject<SyncProgress> _progressSubject = new();

    public IObservable<SyncProgress> Progress => _progressSubject;

    /// <summary>
    /// Performs the initial full enumeration using Graph delta. Pages until exhausted,
    /// persists DriveItemRecords and the final deltaLink for incremental syncs.
    /// </summary>
    public async Task InitialFullSyncAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting initial full sync");
        _progressSubject.OnNext(new SyncProgress
        {
            CurrentOperation = "Starting initial full sync...",
            ProcessedFiles = 0,
            TotalFiles = 0
        });

        string? nextOrDelta = null;
        string? finalDelta = null;
        var pageCount = 0;

        do
        {
            DeltaPage page = await graph.GetDriveDeltaPageAsync(nextOrDelta, ct);
            await repo.ApplyDriveItemsAsync(page.Items, ct);
            nextOrDelta = page.NextLink;
            finalDelta = page.DeltaLink ?? finalDelta;
            pageCount++;

            _progressSubject.OnNext(new SyncProgress
            {
                CurrentOperation = $"Processing delta pages (page {pageCount})",
                ProcessedFiles = pageCount,
                TotalFiles = 0
            });

            logger.LogInformation("Applied page: items={Count} next={Next}", page.Items.Count(), page.NextLink is not null);
        } while(!string.IsNullOrEmpty(nextOrDelta) && !ct.IsCancellationRequested);

        if(!string.IsNullOrEmpty(finalDelta))
        {
            var token = new DeltaToken(Guid.NewGuid().ToString(), finalDelta, DateTimeOffset.UtcNow);
            await repo.SaveOrUpdateDeltaTokenAsync(token, ct);
            logger.LogInformation("Saved delta token");
        }

        _progressSubject.OnNext(new SyncProgress
        {
            CurrentOperation = "Processing transfers...",
            ProcessedFiles = pageCount,
            TotalFiles = pageCount
        });

        // Kick off transfers after DB is updated
        await transfer.ProcessPendingDownloadsAsync(ct);
        await transfer.ProcessPendingUploadsAsync(ct);

        logger.LogInformation("Initial full sync complete");
        _progressSubject.OnNext(new SyncProgress
        {
            CurrentOperation = "Initial sync completed",
            ProcessedFiles = pageCount,
            TotalFiles = pageCount
        });
    }

    /// <summary>
    /// Performs an incremental sync using the stored delta token.
    /// </summary>
    public async Task IncrementalSyncAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting incremental sync");
        _progressSubject.OnNext(new SyncProgress
        {
            CurrentOperation = "Starting incremental sync...",
            ProcessedFiles = 0,
            TotalFiles = 0
        });

        DeltaToken token = await repo.GetDeltaTokenAsync(ct) ?? throw new InvalidOperationException("Delta token missing; run initial sync first.");
        DeltaPage page = await graph.GetDriveDeltaPageAsync(token.Token, ct);
        await repo.ApplyDriveItemsAsync(page.Items, ct);

        if(!string.IsNullOrEmpty(page.DeltaLink))
        {
            await repo.SaveOrUpdateDeltaTokenAsync(token with { Token = page.DeltaLink, LastSyncedUtc = DateTimeOffset.UtcNow }, ct);
            logger.LogInformation("Updated delta token");
        }

        _progressSubject.OnNext(new SyncProgress
        {
            CurrentOperation = "Processing transfers...",
            ProcessedFiles = 1,
            TotalFiles = 1
        });

        await transfer.ProcessPendingDownloadsAsync(ct);
        await transfer.ProcessPendingUploadsAsync(ct);

        logger.LogInformation("Incremental sync complete");
        _progressSubject.OnNext(new SyncProgress
        {
            CurrentOperation = "Incremental sync completed",
            ProcessedFiles = 1,
            TotalFiles = 1
        });
    }
}
