using System.Diagnostics;
using System;
using System.Reactive.Subjects;
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
public sealed class SyncEngine : ISyncEngine
{
    private readonly IDeltaPageProcessor _deltaPageProcessor;
    private readonly ILocalFileScanner _localFileScanner;
    private readonly ITransferService _transfer;
    private readonly ISyncRepository _repo;
    private readonly ILogger<SyncEngine> _logger;
    private readonly Subject<SyncProgress> _progress = new();
    private IDisposable? _transferProgressSubscription;

    public SyncEngine(IDeltaPageProcessor deltaPageProcessor, ILocalFileScanner localFileScanner, ITransferService transfer, ISyncRepository repo, ILogger<SyncEngine> logger)
    {
        _deltaPageProcessor = deltaPageProcessor;
        _localFileScanner = localFileScanner;
        _transfer = transfer;
        _repo = repo;
        _logger = logger;
    }

    private async Task EmitProgressWithStatsAsync(string message, CancellationToken cancellationToken)
    {
        int pendingDownloads = await _repo.GetPendingDownloadCountAsync(cancellationToken);
        int pendingUploads = await _repo.GetPendingUploadCountAsync(cancellationToken);
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
        _logger.LogInformation("[SyncEngine] Starting initial full sync");
        try
        {
            _transferProgressSubscription = _transfer.Progress.Subscribe(_progress.OnNext);
            await EmitProgressWithStatsAsync("Starting delta sync...", cancellationToken);
            _ = await _deltaPageProcessor.ProcessAllDeltaPagesAsync(cancellationToken, progress => _progress.OnNext(progress));
            _logger.LogInformation("[SyncEngine] Delta processing complete, starting downloads");
            await EmitProgressWithStatsAsync("Downloading files...", cancellationToken);
            await _transfer.ProcessPendingDownloadsAsync(cancellationToken);
            await EmitProgressWithStatsAsync("Downloads complete, starting uploads...", cancellationToken);
            _logger.LogInformation("[SyncEngine] Downloads complete, starting uploads");
            await _transfer.ProcessPendingUploadsAsync(cancellationToken);
            stopwatch.Stop();
            _progress.OnNext(new SyncProgress
            {
                OperationType = SyncOperationType.Completed,
                CurrentOperationMessage = $"Initial full sync complete in {stopwatch.ElapsedMilliseconds}ms",
                Timestamp = DateTimeOffset.Now
            });
            _logger.LogInformation("[SyncEngine] Initial full sync complete in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SyncEngine] Initial full sync failed: {Message}", ex.Message);
            _progress.OnNext(new SyncProgress
            {
                OperationType = SyncOperationType.Failed,
                CurrentOperationMessage = $"Initial full sync failed: {ex.Message}",
                Timestamp = DateTimeOffset.Now
            });
            throw;
        }
        finally
        {
            _transferProgressSubscription?.Dispose();
        }
    }

    /// <inheritdoc/>
    public async Task IncrementalSyncAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[SyncEngine] Starting incremental sync");
        try
        {
            _transferProgressSubscription = _transfer.Progress.Subscribe(_progress.OnNext);
            await EmitProgressWithStatsAsync("Starting incremental delta sync...", cancellationToken);
            _ = await _deltaPageProcessor.ProcessAllDeltaPagesAsync(cancellationToken, progress => _progress.OnNext(progress));
            _logger.LogInformation("[SyncEngine] Delta processing complete, starting downloads");
            await EmitProgressWithStatsAsync("Downloading files...", cancellationToken);
            await _transfer.ProcessPendingDownloadsAsync(cancellationToken);
            await EmitProgressWithStatsAsync("Downloads complete, starting uploads...", cancellationToken);
            _logger.LogInformation("[SyncEngine] Downloads complete, starting uploads");
            await _transfer.ProcessPendingUploadsAsync(cancellationToken);
            _progress.OnNext(new SyncProgress
            {
                OperationType = SyncOperationType.Completed,
                CurrentOperationMessage = "Incremental sync complete",
                Timestamp = DateTimeOffset.Now
            });
            _logger.LogInformation("[SyncEngine] Incremental sync complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SyncEngine] Incremental sync failed: {Message}", ex.Message);
            _progress.OnNext(new SyncProgress
            {
                OperationType = SyncOperationType.Failed,
                CurrentOperationMessage = $"Incremental sync failed: {ex.Message}",
                Timestamp = DateTimeOffset.Now
            });
            throw;
        }
        finally
        {
            _transferProgressSubscription?.Dispose();
        }
    }

    /// <inheritdoc/>
    public async Task ScanLocalFilesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[SyncEngine] Starting local file sync");
        try
        {
            _progress.OnNext(new SyncProgress
            {
                OperationType = SyncOperationType.Syncing,
                CurrentOperationMessage = "Scanning local files...",
                Timestamp = DateTimeOffset.Now
            });
            _ = await _localFileScanner.ScanAndSyncLocalFilesAsync(cancellationToken);
            _progress.OnNext(new SyncProgress
            {
                OperationType = SyncOperationType.Completed,
                CurrentOperationMessage = "Local file sync complete",
                Timestamp = DateTimeOffset.Now
            });
            _logger.LogInformation("[SyncEngine] Local file sync complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SyncEngine] Local file sync failed: {Message}", ex.Message);
            _progress.OnNext(new SyncProgress
            {
                OperationType = SyncOperationType.Failed,
                CurrentOperationMessage = $"Local file sync failed: {ex.Message}",
                Timestamp = DateTimeOffset.Now
            });
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken cancellationToken) => null;
}
