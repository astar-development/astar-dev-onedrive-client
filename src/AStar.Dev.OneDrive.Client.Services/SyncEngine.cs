using System.Diagnostics;
using System.Reactive.Subjects;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
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
public sealed class SyncEngine(IDeltaPageProcessor deltaPageProcessor, ILocalFileScanner localFileScanner, ITransferService transfer, ISyncRepository repo, ILogger<SyncEngine> logger) : ISyncEngine
{
    private readonly Subject<SyncProgress> _progress = new();
    private IDisposable? _transferProgressSubscription;

    private async Task EmitProgressWithStatsAsync(string message, CancellationToken cancellationToken)
    {
        var pendingDownloads = await repo.GetPendingDownloadCountAsync(cancellationToken);
        var pendingUploads = await repo.GetPendingUploadCountAsync(cancellationToken);
        _progress.OnNext(new SyncProgress
        {
            OperationType = SyncOperationType.Syncing,
            CurrentOperationMessage = message,
            PendingDownloads = pendingDownloads,
            PendingUploads = pendingUploads,
            Timestamp = DateTimeOffset.Now
        });
    }

    /// <inheritdoc/>
    public IObservable<SyncProgress> Progress => _progress;

    /// <inheritdoc/>
    public async Task InitialFullSyncAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("[SyncEngine] Starting initial full sync");
        try
        {
            _transferProgressSubscription = transfer.Progress.Subscribe(_progress.OnNext);
            await EmitProgressWithStatsAsync("Starting delta sync...", cancellationToken);
            _ = await deltaPageProcessor.ProcessAllDeltaPagesAsync(cancellationToken, progress => _progress.OnNext(progress));
            logger.LogInformation("[SyncEngine] Delta processing complete, starting downloads");
            await EmitProgressWithStatsAsync("Downloading files...", cancellationToken);
            await transfer.ProcessPendingDownloadsAsync(cancellationToken);
            await EmitProgressWithStatsAsync("Downloads complete, starting uploads...", cancellationToken);
            logger.LogInformation("[SyncEngine] Downloads complete, starting uploads");
            await transfer.ProcessPendingUploadsAsync(cancellationToken);
            stopwatch.Stop();
            _progress.OnNext(new SyncProgress
            {
                OperationType = SyncOperationType.Completed,
                CurrentOperationMessage = $"Initial full sync complete in {stopwatch.ElapsedMilliseconds}ms",
                Timestamp = DateTimeOffset.Now
            });
            logger.LogInformation("[SyncEngine] Initial full sync complete in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "[SyncEngine] Initial full sync failed: {Message}", ex.Message);
            _progress.OnNext(new SyncProgress
            {
                OperationType = SyncOperationType.Failed,
                CurrentOperationMessage = $"Initial full sync failed: {ex.Message}",
                Timestamp = DateTimeOffset.Now
            });

            throw new IOException("Initial full sync failed", ex);
        }
        finally
        {
            _transferProgressSubscription?.Dispose();
        }
    }

    /// <inheritdoc/>
    public async Task IncrementalSyncAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("[SyncEngine] Starting incremental sync");
        try
        {
            _transferProgressSubscription = transfer.Progress.Subscribe(_progress.OnNext);
            await EmitProgressWithStatsAsync("Starting incremental delta sync...", cancellationToken);
            _ = await deltaPageProcessor.ProcessAllDeltaPagesAsync(cancellationToken, progress => _progress.OnNext(progress));
            logger.LogInformation("[SyncEngine] Delta processing complete, starting downloads");
            await EmitProgressWithStatsAsync("Downloading files...", cancellationToken);
            await transfer.ProcessPendingDownloadsAsync(cancellationToken);
            await EmitProgressWithStatsAsync("Downloads complete, starting uploads...", cancellationToken);
            logger.LogInformation("[SyncEngine] Downloads complete, starting uploads");
            await transfer.ProcessPendingUploadsAsync(cancellationToken);
            _progress.OnNext(new SyncProgress
            {
                OperationType = SyncOperationType.Completed,
                CurrentOperationMessage = "Incremental sync complete",
                Timestamp = DateTimeOffset.Now
            });
            logger.LogInformation("[SyncEngine] Incremental sync complete");
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "[SyncEngine] Incremental sync failed: {Message}", ex.Message);
            _progress.OnNext(new SyncProgress
            {
                OperationType = SyncOperationType.Failed,
                CurrentOperationMessage = $"Incremental sync failed: {ex.Message}",
                Timestamp = DateTimeOffset.Now
            });

            throw new IOException("Incremental sync failed", ex);
        }
        finally
        {
            _transferProgressSubscription?.Dispose();
        }
    }

    /// <inheritdoc/>
    public async Task ScanLocalFilesAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("[SyncEngine] Starting local file sync");
        try
        {
            _progress.OnNext(new SyncProgress
            {
                OperationType = SyncOperationType.Syncing,
                CurrentOperationMessage = "Scanning local files...",
                Timestamp = DateTimeOffset.Now
            });
            _ = await localFileScanner.ScanAndSyncLocalFilesAsync(cancellationToken);
            _progress.OnNext(new SyncProgress
            {
                OperationType = SyncOperationType.Completed,
                CurrentOperationMessage = "Local file sync complete",
                Timestamp = DateTimeOffset.Now
            });
            logger.LogInformation("[SyncEngine] Local file sync complete");
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "[SyncEngine] Local file sync failed: {Message}", ex.Message);
            _progress.OnNext(new SyncProgress
            {
                OperationType = SyncOperationType.Failed,
                CurrentOperationMessage = $"Local file sync failed: {ex.Message}",
                Timestamp = DateTimeOffset.Now
            });

            throw new IOException("Local file sync failed", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken cancellationToken) => await repo.GetDeltaTokenAsync(cancellationToken);
}
