using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Services;

public sealed class SyncEngine
{
    private readonly ISyncRepository _repo;
    private readonly IGraphClient _graph;
    private readonly TransferService _transfer;
    private readonly ILogger<SyncEngine> _logger;

    public SyncEngine(ISyncRepository repo, IGraphClient graph, TransferService transfer, ILogger<SyncEngine> logger)
    {
        _repo = repo;
        _graph = graph;
        _transfer = transfer;
        _logger = logger;
    }

    /// <summary>
    /// Performs the initial full enumeration using Graph delta. Pages until exhausted,
    /// persists DriveItemRecords and the final deltaLink for incremental syncs.
    /// </summary>
    public async Task InitialFullSyncAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting initial full sync");
        string? nextOrDelta = null;
        string? finalDelta = null;

        do
        {
            DeltaPage page = await _graph.GetDriveDeltaPageAsync(nextOrDelta, ct);
            await _repo.ApplyDriveItemsAsync(page.Items, ct);
            nextOrDelta = page.NextLink;
            finalDelta = page.DeltaLink ?? finalDelta;
            _logger.LogInformation("Applied page: items={Count} next={Next}", page.Items.Count(), page.NextLink is not null);
        } while(!string.IsNullOrEmpty(nextOrDelta) && !ct.IsCancellationRequested);

        if(!string.IsNullOrEmpty(finalDelta))
        {
            var token = new DeltaToken(Guid.NewGuid().ToString(), finalDelta, DateTimeOffset.UtcNow);
            await _repo.SaveOrUpdateDeltaTokenAsync(token, ct);
            _logger.LogInformation("Saved delta token");
        }

        // Kick off transfers after DB is updated
        await _transfer.ProcessPendingDownloadsAsync(ct);
        await _transfer.ProcessPendingUploadsAsync(ct);
        _logger.LogInformation("Initial full sync complete");
    }

    /// <summary>
    /// Performs an incremental sync using the stored delta token.
    /// </summary>
    public async Task IncrementalSyncAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting incremental sync");
        DeltaToken token = await _repo.GetDeltaTokenAsync(ct) ?? throw new InvalidOperationException("Delta token missing; run initial sync first.");
        DeltaPage page = await _graph.GetDriveDeltaPageAsync(token.Token, ct);
        await _repo.ApplyDriveItemsAsync(page.Items, ct);

        if(!string.IsNullOrEmpty(page.DeltaLink))
        {
            await _repo.SaveOrUpdateDeltaTokenAsync(token with { Token = page.DeltaLink, LastSyncedUtc = DateTimeOffset.UtcNow }, ct);
            _logger.LogInformation("Updated delta token");
        }

        await _transfer.ProcessPendingDownloadsAsync(ct);
        await _transfer.ProcessPendingUploadsAsync(ct);
        _logger.LogInformation("Incremental sync complete");
    }
}
