using System.Diagnostics;
using System.IO.Abstractions;
using System.Reactive.Subjects;
using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using Microsoft.Extensions.Logging;
using Polly;

namespace AStar.Dev.OneDrive.Client.Services;

public class TransferService : ITransferService
{
    private readonly IFileSystemAdapter _fs;
    private readonly IGraphClient _graph;
    private readonly ISyncRepository _repo;
    private readonly ILogger<TransferService> _logger;
    private readonly UserPreferences _settings;
    private readonly SemaphoreSlim _downloadSemaphore;
    private readonly AsyncPolicy _retryPolicy;
    private readonly Subject<SyncProgress> _progressSubject = new();
    private readonly Stopwatch _operationStopwatch = new();
    private long _totalBytesTransferred;

    public IObservable<SyncProgress> Progress => _progressSubject;

    public TransferService(IFileSystemAdapter fs, IGraphClient graph, ISyncRepository repo, ILogger<TransferService> logger, UserPreferences settings)
    {
        _fs = fs;
        _graph = graph;
        _repo = repo;
        _logger = logger;
        _settings = settings;
        _downloadSemaphore = new SemaphoreSlim(settings.UiSettings.SyncSettings.MaxParallelDownloads);

        _retryPolicy = Policy.TimeoutAsync(TimeSpan.FromMinutes(5))
            .WrapAsync(Policy.Handle<HttpRequestException>()
                             .Or<IOException>()
                             .WaitAndRetryAsync(
                                 settings.UiSettings.SyncSettings.MaxRetries,
                                 retryAttempt => TimeSpan.FromMilliseconds(settings.UiSettings.SyncSettings.RetryBaseDelayMs * Math.Pow(2, retryAttempt)),
                                 (ex, ts, retryCount, ctx) =>
                                 {
                                     var exceptionType = ex.GetType().Name;
                                     var isNetworkError = ex is IOException || (ex is HttpRequestException && ex.InnerException is IOException);
                                     var errorCategory = isNetworkError ? "Network I/O" : exceptionType;

                                     _logger.LogWarning(ex,
                                         "[{ErrorCategory}] Retry {Retry}/{MaxRetries} after {Delay}ms. Error: {Message}",
                                         errorCategory, retryCount, settings.UiSettings.SyncSettings.MaxRetries,
                                         ts.TotalMilliseconds, ex.Message);
                                 }));
    }

    /// <summary>
    /// Pulls pending downloads from repository in batches and downloads them with bounded concurrency.
    /// </summary>
    public async Task ProcessPendingDownloadsAsync(CancellationToken cancellationToken)
    {
        _operationStopwatch.Restart();
        _totalBytesTransferred = 0;
        _logger.LogInformation("Processing pending downloads");
        var totalProcessed = 0;
        var pageCount = 0;
        var batchSize = GetDownloadBatchSize();
        var total = await _repo.GetPendingDownloadCountAsync(cancellationToken);

        _logger.LogInformation("Found {TotalPending} pending downloads (batch size: {BatchSize})", total, batchSize);

        if(NoPendingDownloads(total))
        {
            _logger.LogInformation("No pending downloads found - sync complete");
            return;
        }

        long totalBytesForAllDownloads = 0;
        var batchProcessed = true;
        try
        {
            while(!cancellationToken.IsCancellationRequested && batchProcessed)
            {
                cancellationToken.ThrowIfCancellationRequested();
                (var BatchProcessed, var PageCount, var TotalProcessed, var TotalBytesForAllDownloads) = await ProcessDownloadBatchAsync(
                    batchSize, total, pageCount, totalProcessed, totalBytesForAllDownloads, cancellationToken);
                batchProcessed = BatchProcessed;
                pageCount = PageCount;
                totalProcessed = TotalProcessed;
                totalBytesForAllDownloads = TotalBytesForAllDownloads;
            }

            if(cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Download process cancelled after {Processed}/{Total} files", totalProcessed, total);
                ReportSyncCancelled("Download cancelled");
            }
            else
            {
                _operationStopwatch.Stop();
                _logger.LogInformation("Completed downloads: {Processed} files, {TotalMB:F2} MB in {ElapsedSec:F2}s ({SpeedMBps:F2} MB/s)",
                    totalProcessed, _totalBytesTransferred / (1024.0 * 1024.0), _operationStopwatch.Elapsed.TotalSeconds,
                    _totalBytesTransferred / (1024.0 * 1024.0) / _operationStopwatch.Elapsed.TotalSeconds);
            }
        }
        catch(OperationCanceledException)
        {
#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
            _logger.LogWarning("Download process cancelled by user");
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.
            ReportSyncCancelled("Download cancelled");
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Download process failed: {Message}", ex.Message);
            ReportSyncFailed($"Download failed: {ex.Message}");
            throw new IOException("Download process failed. See inner exception for details.", ex);
        }
    }

    private async Task<(bool BatchProcessed, int PageCount, int TotalProcessed, long TotalBytesForAllDownloads)> ProcessDownloadBatchAsync(
        int batchSize,
        int total,
        int pageCount,
        int totalProcessed,
        long totalBytesForAllDownloads,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching batch {PageNumber} (offset: {Offset})", pageCount + 1, pageCount * batchSize);
        var items = (await _repo.GetPendingDownloadsAsync(batchSize, pageCount, cancellationToken)).ToList();
        pageCount++;

        if(NoMoreItemsInBatch(items))
        {
            _logger.LogInformation("No more items in batch - all downloads complete");
            return (false, pageCount, totalProcessed, totalBytesForAllDownloads);
        }

        _logger.LogInformation("Processing batch {BatchNumber} with {ItemCount} files", pageCount, items.Count);

        // First batch: calculate total bytes for progress tracking
        totalBytesForAllDownloads = EstimateTotalDownloadBytesIfFirstBatch(items, total, totalBytesForAllDownloads, pageCount);

        totalProcessed = await ExecuteDownloadTasksAsync(items, total, totalBytesForAllDownloads, totalProcessed, cancellationToken);

        _logger.LogInformation("Batch {BatchNumber} complete: {Processed}/{Total} files downloaded so far",
            pageCount, totalProcessed, total);
        return (true, pageCount, totalProcessed, totalBytesForAllDownloads);
    }

    /// <summary>
    /// Scans repository for pending uploads and uploads them using upload sessions and chunked uploads.
    /// </summary>
    public async Task ProcessPendingUploadsAsync(CancellationToken cancellationToken)
    {
        _operationStopwatch.Restart();
        _totalBytesTransferred = 0;
        _logger.LogInformation("Processing pending uploads");
        var uploads = (await _repo.GetPendingUploadsAsync(_settings.UiSettings.SyncSettings.DownloadBatchSize, cancellationToken)).ToList();
        var totalProcessed = 0;
        var pendingUploads = uploads.Count;
        var totalBytes = uploads.Sum(u => u.Size);

        totalProcessed = await ProcessUploadBatchAsync(uploads, pendingUploads, totalBytes, cancellationToken);

        _operationStopwatch.Stop();
        _logger.LogInformation("Completed uploads: {Processed} files, {TotalMB:F2} MB in {ElapsedSec:F2}s ({SpeedMBps:F2} MB/s)",
            totalProcessed, _totalBytesTransferred / (1024.0 * 1024.0), _operationStopwatch.Elapsed.TotalSeconds,
            _totalBytesTransferred / (1024.0 * 1024.0) / _operationStopwatch.Elapsed.TotalSeconds);
    }

    private long EstimateTotalDownloadBytesIfFirstBatch(
        ICollection<DriveItemRecord> items,
        int total,
        long totalBytesForAllDownloads,
        int pageCount)
    {
        if(ShouldEstimateTotalBytes(totalBytesForAllDownloads, pageCount))
        {
            var avgSize = GetAverageFileSize(items);
            totalBytesForAllDownloads = avgSize * total;
            _logger.LogInformation("Estimated total download size: {TotalMB:F2} MB (avg file size: {AvgKB:F2} KB)",
                totalBytesForAllDownloads / (1024.0 * 1024.0), avgSize / 1024.0);
        }

        return totalBytesForAllDownloads;
    }

    private async Task<int> ExecuteDownloadTasksAsync(
    List<DriveItemRecord> items,
    int total,
    long totalBytesForAllDownloads,
    int totalProcessed,
    CancellationToken cancellationToken)
    {
        var processed = totalProcessed;
        var maxParallel = Math.Max(1, _settings.UiSettings.SyncSettings.MaxParallelDownloads);
        using var throttler = new SemaphoreSlim(maxParallel);

        var tasks = items.Select(async item =>
        {
            await throttler.WaitAsync(cancellationToken);
            try
            {
                await DownloadItemWithRetryAsync(item, cancellationToken, () =>
                {
                    var p = Interlocked.Increment(ref processed);
                    _ = Interlocked.Add(ref _totalBytesTransferred, item.Size);
                    ReportProgress(p, "Downloading files", total, 0, totalBytesForAllDownloads);
                });
            }
            catch (OperationCanceledException)
            {
                // Optionally log or handle per-task cancellation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Download failed for {Path}", item.RelativePath);
            }
            finally
            {
                _ = throttler.Release();
            }
        }).ToList();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch(OperationCanceledException)
        {
#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
            _logger.LogWarning("Download tasks cancelled by user");
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.
            throw;
        }

        return processed;
    }

    private async Task<int> ProcessUploadBatchAsync(
        List<LocalFileRecord> uploads,
        int pendingUploads,
        long totalBytes,
        CancellationToken cancellationToken)
    {
        var totalProcessed = 0;
        foreach(LocalFileRecord? local in uploads)
        {
            await _retryPolicy.ExecuteAsync(async cancellationToken =>
            {
                await UploadLocalFileAsync(local, cancellationToken);
                var processed = Interlocked.Increment(ref totalProcessed);
                _ = Interlocked.Add(ref _totalBytesTransferred, local.Size);
                ReportProgress(processed, "Uploading files", uploads.Count, pendingUploads, totalBytes);
            }, cancellationToken);
        }

        return totalProcessed;
    }

    private async Task UploadLocalFileAsync(LocalFileRecord local, CancellationToken cancellationToken)
    {
        var log = new TransferLog(Guid.CreateVersion7().ToString(), TransferType.Upload, local.Id, DateTimeOffset.UtcNow, null, TransferStatus.InProgress, 0, null);
        await _repo.LogTransferAsync(log, cancellationToken);
        try
        {
            var parent = Path.GetDirectoryName(local.RelativePath) ?? "/";
            var fileName = Path.GetFileName(local.RelativePath);
            UploadSessionInfo session = await _graph.CreateUploadSessionAsync(parent, fileName, cancellationToken);

            await using Stream stream = await _fs.OpenReadAsync(local.RelativePath, cancellationToken) ?? throw new FileNotFoundException(local.RelativePath);
            const int chunkSize = 320 * 1024; // 320KB
            long uploaded = 0;
            while(uploaded < stream.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var toRead = (int)Math.Min(chunkSize, stream.Length - uploaded);
                var buffer = new byte[toRead];
                _ = stream.Seek(uploaded, SeekOrigin.Begin);
                var read = await stream.ReadAsync(buffer.AsMemory(0, toRead), cancellationToken);
                await using var ms = new MemoryStream(buffer, 0, read, writable: false);
                await _graph.UploadChunkAsync(session, ms, uploaded, uploaded + read - 1, cancellationToken);
                uploaded += read;
            }

            await _repo.MarkLocalFileStateAsync(local.Id, SyncState.Uploaded, cancellationToken);
            log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Success, BytesTransferred = stream.Length };
            await _repo.LogTransferAsync(log, cancellationToken);
            _logger.LogInformation("Uploaded {Path}", local.RelativePath);
        }
        catch(OperationCanceledException)
        {
#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
            _logger.LogWarning("Upload cancelled for {Id}", local.Id);
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Upload failed for {Id}", local.Id);
            log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Failed, Error = ex.Message, BytesTransferred = 0 };
            await _repo.LogTransferAsync(log, cancellationToken);
        }
    }

    private async Task DownloadItemWithRetryAsync(DriveItemRecord item, CancellationToken cancellationToken, Action? onComplete = null)
    {
        try
        {
            await _retryPolicy.ExecuteAsync(async _ =>
            {
                await DownloadItemAsync(item, cancellationToken);
                onComplete?.Invoke();
            }, cancellationToken);
        }
        catch(Exception ex)
        {
            var exceptionType = ex.GetType().Name;

            var isNetworkError = IsNetworkError(ex);
            var errorDetail = GetErrorDetail(isNetworkError, exceptionType, ex);

            _logger.LogError(ex, "Failed to download {Path} after {MaxRetries} retries. {ErrorDetail}",
                item.RelativePath, _settings.UiSettings.SyncSettings.MaxRetries, errorDetail);
            throw new IOException($"Download failed for {item.RelativePath} after {_settings.UiSettings.SyncSettings.MaxRetries} retries. See inner exception for details.", ex);
        }
    }

    private async Task DownloadItemAsync(DriveItemRecord item, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting download: {Path} ({SizeKB:F2} KB)", item.RelativePath, item.Size / 1024.0);
        _logger.LogDebug("Waiting to acquire semaphore for {Path}", item.RelativePath);
        await _downloadSemaphore.WaitAsync(cancellationToken);
        _logger.LogDebug("Semaphore acquired for {Path}", item.RelativePath);

        var log = new TransferLog(Guid.CreateVersion7().ToString(), TransferType.Download, item.Id, DateTimeOffset.UtcNow, null, TransferStatus.InProgress, item.Size, null);
        await _repo.LogTransferAsync(log, cancellationToken);
        try
        {
            _logger.LogDebug("Downloading content for: {Path}", item.RelativePath);
            await using Stream stream = await _graph.DownloadDriveItemContentAsync(item.DriveItemId, cancellationToken);

            _logger.LogDebug("Writing file to disk: {Path}", item.RelativePath);

            // Enhanced: Write file in chunks and log progress for semi-large files
            const long logIntervalBytes = 10 * 1024 * 1024; // 10 MB
            long totalWritten = 0;
            var nextLogThreshold = logIntervalBytes;
            await using Stream output = await _fs.OpenWriteAsync(item.RelativePath, cancellationToken);
            var buffer = new byte[1024 * 1024]; // 1 MB buffer
            int read;
            while((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Read {Bytes} bytes from stream for {Path}", read, item.RelativePath);
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                totalWritten += read;

                if(item.Size > logIntervalBytes && totalWritten >= nextLogThreshold)
                {
                    _logger.LogInformation("Downloading {Path}: {MB:F2} MB of {TotalMB:F2} MB complete",
                        item.RelativePath, totalWritten / (1024.0 * 1024.0), item.Size / (1024.0 * 1024.0));
                    // Send progress update to UI
                    ReportProgress(
                        processed: 0, // You may want to pass the correct processed count here
                        operation: $"Downloading {item.RelativePath} ({totalWritten / (1024.0 * 1024.0):F2} MB / {item.Size / (1024.0 * 1024.0):F2} MB)",
                        total: 0, // Or the correct total
                        pendingUploads: 0,
                        totalBytes: item.Size
                    );
                    nextLogThreshold += logIntervalBytes;
                }
            }

            _logger.LogDebug("Completed reading stream for {Path}", item.RelativePath);

            _logger.LogDebug("Marking file as downloaded: {Path}", item.RelativePath);
            await _repo.MarkLocalFileStateAsync(item.Id, SyncState.Downloaded, cancellationToken);

            IFileInfo fileInfo = _fs.GetFileInfo(item.RelativePath);
            log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Success, BytesTransferred = fileInfo.Length };
            await _repo.LogTransferAsync(log, cancellationToken);
            _logger.LogInformation("Downloaded {Path} ({SizeKB:F2} KB)", item.RelativePath, fileInfo.Length / 1024.0);
        }
        catch(Exception ex)
        {
            var exceptionType = ex.GetType().Name;
            var isNetworkError = IsNetworkError(ex);
            _logger.LogError(ex, "Download failed for {Path} (DriveItemId: {DriveItemId}). Type: {ExceptionType}, Network Error: {IsNetwork}",
                item.RelativePath, item.DriveItemId, exceptionType, isNetworkError);

            log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Failed, Error = ex.Message, BytesTransferred = 0 };
            await _repo.LogTransferAsync(log, cancellationToken);
            throw new IOException($"Download failed for {item.RelativePath} after {_settings.UiSettings.SyncSettings.MaxRetries} retries. See inner exception for details.", ex);
        }
        finally
        {
            _logger.LogDebug("Releasing semaphore for {Path}", item.RelativePath);
            _ = _downloadSemaphore.Release();
        }
    }

    private void ReportProgress(int processed, string operation, int total = 0, int pendingUploads = 0, long totalBytes = 0)
    {
        var elapsedSeconds = _operationStopwatch.Elapsed.TotalSeconds;
        var bytesPerSecond = elapsedSeconds > 0 ? _totalBytesTransferred / elapsedSeconds : 0;

        TimeSpan? eta = null;
        if(ShouldCalculateEta(bytesPerSecond, totalBytes, _totalBytesTransferred))
        {
            var remainingBytes = totalBytes - _totalBytesTransferred;
            var remainingSeconds = remainingBytes / bytesPerSecond;
            eta = TimeSpan.FromSeconds(remainingSeconds);
        }

        var syncProgress = new SyncProgress
        {
            OperationType = SyncOperationType.Syncing,
            CurrentOperationMessage = operation,
            ProcessedFiles = processed,
            TotalFiles = total,
            PendingDownloads = total - processed,
            PendingUploads = pendingUploads,
            BytesTransferred = _totalBytesTransferred,
            TotalBytes = totalBytes,
            BytesPerSecond = bytesPerSecond,
            EstimatedTimeRemaining = eta,
            ElapsedTime = _operationStopwatch.Elapsed
        };

        _progressSubject.OnNext(syncProgress);
    }

    private void ReportSyncFailed(string message)
    {
        var syncProgress = new SyncProgress
        {
            OperationType = SyncOperationType.Failed,
            CurrentOperationMessage = message,
            ProcessedFiles = 0,
            TotalFiles = 0,
            PendingDownloads = 0,
            PendingUploads = 0,
            BytesTransferred = 0,
            TotalBytes = 0,
            BytesPerSecond = 0,
            EstimatedTimeRemaining = null,
            ElapsedTime = _operationStopwatch.Elapsed
        };
        _progressSubject.OnNext(syncProgress);
    }

    private void ReportSyncCancelled(string message)
    {
        var syncProgress = new SyncProgress
        {
            OperationType = SyncOperationType.Cancelled,
            CurrentOperationMessage = message,
            ProcessedFiles = 0,
            TotalFiles = 0,
            PendingDownloads = 0,
            PendingUploads = 0,
            BytesTransferred = 0,
            TotalBytes = 0,
            BytesPerSecond = 0,
            EstimatedTimeRemaining = null,
            ElapsedTime = _operationStopwatch.Elapsed
        };
        _progressSubject.OnNext(syncProgress);
    }

    // --- Extracted helper methods for readability and maintainability ---

    private int GetDownloadBatchSize() => _settings.UiSettings.SyncSettings.DownloadBatchSize > 0
        ? _settings.UiSettings.SyncSettings.DownloadBatchSize
        : 100;

    private static bool NoPendingDownloads(int total) => total == 0;

    private static bool NoMoreItemsInBatch(List<DriveItemRecord> items) => items.Count == 0;

    private static bool ShouldEstimateTotalBytes(long totalBytesForAllDownloads, int pageCount)
        => totalBytesForAllDownloads == 0 && pageCount == 1;

    private static long GetAverageFileSize(ICollection<DriveItemRecord> items)
        => items.Count != 0 ? (long)items.Average(i => i.Size) : 0;

    private static bool IsNetworkError(Exception ex)
        => ex is IOException || (ex is HttpRequestException hre && hre.InnerException is IOException);

    private static string GetErrorDetail(bool isNetworkError, string exceptionType, Exception ex)
        => isNetworkError
            ? "Network connection error - connection was forcibly closed or timed out"
            : $"{exceptionType}: {ex.Message}";

    private static bool ShouldCalculateEta(double bytesPerSecond, long totalBytes, long totalBytesTransferred)
        => bytesPerSecond > 0 && totalBytes > 0 && totalBytesTransferred < totalBytes;
}
