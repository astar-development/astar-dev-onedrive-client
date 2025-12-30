using System.Diagnostics;
using AStar.Dev.OneDrive.Client.Core.Entities;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Services;

/// <summary>
/// Orchestrates synchronization between local storage and OneDrive by delegating to service abstractions.
/// </summary>
/// <remarks>
/// Coordinates delta page processing, local file scanning, and file transfers. Contains no business logic.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="SyncEngine"/> class.
/// </remarks>
/// <param name="deltaPageProcessor">The delta page processor abstraction.</param>
/// <param name="localFileScanner">The local file scanner abstraction.</param>
/// <param name="transfer">The transfer service abstraction.</param>
/// <param name="logger">The logger instance.</param>
public sealed class SyncEngine(IDeltaPageProcessor deltaPageProcessor, ILocalFileScanner localFileScanner, ITransferService transfer, ILogger<SyncEngine> logger) : ISyncEngine
{

    /// <inheritdoc/>
    public IObservable<SyncProgress> Progress => System.Reactive.Linq.Observable.Empty<SyncProgress>();

    /// <inheritdoc/>
    public async Task InitialFullSyncAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Starting initial full sync");
        _ = await deltaPageProcessor.ProcessAllDeltaPagesAsync(cancellationToken);
        await transfer.ProcessPendingDownloadsAsync(cancellationToken);
        await transfer.ProcessPendingUploadsAsync(cancellationToken);
        stopwatch.Stop();
        logger.LogInformation("Initial full sync complete in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
    }

    /// <inheritdoc/>
    public async Task IncrementalSyncAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting incremental sync");
        _ = await deltaPageProcessor.ProcessAllDeltaPagesAsync(cancellationToken);
        await transfer.ProcessPendingDownloadsAsync(cancellationToken);
        await transfer.ProcessPendingUploadsAsync(cancellationToken);
        logger.LogInformation("Incremental sync complete");
    }

    /// <inheritdoc/>
    public async Task ScanLocalFilesAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting local file sync");
        _ = await localFileScanner.ScanAndSyncLocalFilesAsync(cancellationToken);
        logger.LogInformation("Local file sync complete");
    }

    /// <inheritdoc/>
    public async Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken cancellationToken) => null;
}
