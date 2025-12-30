using System.Diagnostics;
using System.IO.Abstractions;
using System.Threading.Channels;
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
    private readonly Stopwatch _operationStopwatch = new();
    private long _totalBytesTransferred;

    private readonly SyncProgressReporter _progressReporter;
    private readonly ISyncErrorLogger _errorLogger;
    private readonly IChannelFactory _channelFactory;
    private readonly IDownloadQueueProducer _producer;
    private readonly IDownloadQueueConsumer _consumer;
    private readonly IUploadQueueProducer _uploadProducer;
    private readonly IUploadQueueConsumer _uploadConsumer;

    public IObservable<SyncProgress> Progress { get; }

    public TransferService(
        IFileSystemAdapter fs,
        IGraphClient graph,
        ISyncRepository repo,
        ILogger<TransferService> logger,
        UserPreferences settings,
        SyncProgressReporter progressReporter,
        ISyncErrorLogger errorLogger,
        IChannelFactory channelFactory,
        IDownloadQueueProducer producer,
        IDownloadQueueConsumer consumer,
        IUploadQueueProducer uploadProducer,
        IUploadQueueConsumer uploadConsumer)
    {
        _fs = fs;
        _graph = graph;
        _repo = repo;
        _logger = logger;
        _settings = settings;
        _progressReporter = progressReporter;
        _errorLogger = errorLogger;
        _channelFactory = channelFactory;
        _producer = producer;
        _consumer = consumer;
        _uploadProducer = uploadProducer;
        _uploadConsumer = uploadConsumer;
        _downloadSemaphore = new SemaphoreSlim(settings.UiSettings.SyncSettings.MaxParallelDownloads);

        Progress = progressReporter.Progress;

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
        var batchSize = _settings.UiSettings.SyncSettings.DownloadBatchSize > 0
            ? _settings.UiSettings.SyncSettings.DownloadBatchSize
            : 100;
        var total = await _repo.GetPendingDownloadCountAsync(cancellationToken);

        _logger.LogInformation("Found {TotalPending} pending downloads (batch size: {BatchSize})", total, batchSize);

        if(total == 0)
        {
            _logger.LogInformation("No pending downloads found - sync complete");
            return;
        }

        Channel<DriveItemRecord> channel = _channelFactory.CreateBounded(_settings.UiSettings.SyncSettings.MaxParallelDownloads * 2);
        // Start producer and consumer tasks
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        ChannelWriter<DriveItemRecord> writer = channel.Writer;
        ChannelReader<DriveItemRecord> reader = channel.Reader;

        var processedCount = 0;
        IEnumerable<DriveItemRecord> allPendingDownloads = await _repo.GetAllPendingDownloadsAsync(cancellationToken);
        var totalBytesForAllDownloads = allPendingDownloads.Sum(i => i.Size);
        var parallelism = Math.Max(1, _settings.UiSettings.SyncSettings.MaxParallelDownloads);

        async Task processItemAsync(DriveItemRecord item)
        {
            try
            {
                await DownloadItemWithRetryAsync(item, cancellationToken, (b, e, eta) =>
                {
                    // Optionally emit chunked progress here if desired
                });
                var p = Interlocked.Increment(ref processedCount);
                var totalTransferred = Interlocked.Add(ref _totalBytesTransferred, item.Size);
                var elapsedSeconds = _operationStopwatch.Elapsed.TotalSeconds;
                var bytesPerSecond = elapsedSeconds > 0 ? totalTransferred / elapsedSeconds : 0;
                TimeSpan? eta = null;
                if(bytesPerSecond > 0 && totalTransferred < totalBytesForAllDownloads)
                {
                    var remainingBytes = totalBytesForAllDownloads - totalTransferred;
                    var remainingSeconds = remainingBytes / bytesPerSecond;
                    eta = TimeSpan.FromSeconds(remainingSeconds);
                }

                _progressReporter.Report(new SyncProgress
                {
                    OperationType = SyncOperationType.Syncing,
                    CurrentOperationMessage = $"Downloading \"{item.RelativePath}\"",
                    ProcessedFiles = p,
                    TotalFiles = total,
                    PendingDownloads = total - p,
                    PendingUploads = 0,
                    BytesTransferred = totalTransferred,
                    TotalBytes = totalBytesForAllDownloads,
                    BytesPerSecond = bytesPerSecond,
                    EstimatedTimeRemaining = eta,
                    ElapsedTime = _operationStopwatch.Elapsed
                });
            }
            catch(Exception ex)
            {
                _errorLogger.LogError(ex, item.RelativePath);
            }
        }

        Task producerTask = _producer.ProduceAsync(writer, cts.Token);
        Task consumerTask = _consumer.ConsumeAsync(reader, processItemAsync, parallelism, cts.Token);

        // Wait for completion
        try
        {
            await Task.WhenAll(producerTask, consumerTask);
        }
        catch(OperationCanceledException)
        {
            // Handle cancellation
#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
            _logger.LogWarning("Download processing was cancelled");
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.
        }
        finally
        {
            await cts.CancelAsync();
            cts.Dispose();
        }

        _logger.LogInformation("All pending downloads have been processed");
    }

    /// <summary>
    /// Scans repository for pending uploads and uploads them using upload sessions and chunked uploads.
    /// </summary>
    public async Task ProcessPendingUploadsAsync(CancellationToken cancellationToken)
    {
        _operationStopwatch.Restart();
        _totalBytesTransferred = 0;
        _logger.LogInformation("Processing pending uploads");
        var allPendingUploads = (await _repo.GetPendingUploadsAsync(int.MaxValue, cancellationToken)).ToList();
        var total = allPendingUploads.Count;
        var totalBytesForAllUploads = allPendingUploads.Sum(u => u.Size);
        if (total == 0)
        {
            _logger.LogInformation("No pending uploads found - sync complete");
            return;
        }

        Channel<LocalFileRecord> channel = _channelFactory.CreateBounded(_settings.UiSettings.SyncSettings.MaxParallelDownloads * 2);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        ChannelWriter<LocalFileRecord> writer = channel.Writer;
        ChannelReader<LocalFileRecord> reader = channel.Reader;
        var processedCount = 0;
        var parallelism = Math.Max(1, _settings.UiSettings.SyncSettings.MaxParallelDownloads);

        async Task processItemAsync(LocalFileRecord item)
        {
            try
            {
                var fileStopwatch = Stopwatch.StartNew();
                await UploadLocalFileWithRetryAsync(item, cancellationToken, (b, e, eta) =>
                {
                    // Optionally emit chunked progress here if desired
                });
                fileStopwatch.Stop();
                var p = Interlocked.Increment(ref processedCount);
                var totalTransferred = Interlocked.Add(ref _totalBytesTransferred, item.Size);
                var elapsedSeconds = _operationStopwatch.Elapsed.TotalSeconds;
                var bytesPerSecond = elapsedSeconds > 0 ? totalTransferred / elapsedSeconds : 0;
                TimeSpan? eta = null;
                if (bytesPerSecond > 0 && totalTransferred < totalBytesForAllUploads)
                {
                    var remainingBytes = totalBytesForAllUploads - totalTransferred;
                    var remainingSeconds = remainingBytes / bytesPerSecond;
                    eta = TimeSpan.FromSeconds(remainingSeconds);
                }
                _progressReporter.Report(new SyncProgress
                {
                    OperationType = SyncOperationType.Syncing,
                    CurrentOperationMessage = $"Uploading \"{item.RelativePath}\"",
                    ProcessedFiles = p,
                    TotalFiles = total,
                    PendingDownloads = 0,
                    PendingUploads = total - p,
                    BytesTransferred = totalTransferred,
                    TotalBytes = totalBytesForAllUploads,
                    BytesPerSecond = bytesPerSecond,
                    EstimatedTimeRemaining = eta,
                    ElapsedTime = _operationStopwatch.Elapsed
                });
            }
            catch(Exception ex)
            {
                _errorLogger.LogError(ex, item.RelativePath);
            }
        }

        Task producerTask = _uploadProducer.ProduceAsync(writer, cts.Token);
        Task consumerTask = _uploadConsumer.ConsumeAsync(reader, processItemAsync, parallelism, cts.Token);

        try
        {
            await Task.WhenAll(producerTask, consumerTask);
        }
        catch(OperationCanceledException)
        {
            _logger.LogWarning("Upload processing was cancelled");
        }
        finally
        {
            await cts.CancelAsync();
            cts.Dispose();
        }

        _operationStopwatch.Stop();
        _logger.LogInformation("All pending uploads have been processed");
    }


    private async Task UploadLocalFileWithRetryAsync(LocalFileRecord local, CancellationToken cancellationToken, Action<long, TimeSpan, TimeSpan?>? onProgress = null)
    {
        try
        {
            await _retryPolicy.ExecuteAsync(async _ => await UploadLocalFileAsync(local, cancellationToken, onProgress), cancellationToken);
        }
        catch(Exception ex)
        {
            _errorLogger.LogError(ex, local.RelativePath);
            throw new IOException($"Upload failed for {local.RelativePath} after {_settings.UiSettings.SyncSettings.MaxRetries} retries. See inner exception for details.", ex);
        }
    }

    private async Task UploadLocalFileAsync(LocalFileRecord local, CancellationToken cancellationToken, Action<long, TimeSpan, TimeSpan?>? onProgress = null)
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
            var fileStopwatch = Stopwatch.StartNew();
            const long logIntervalBytes = 10 * 1024 * 1024; // 10 MB
            long nextLogThreshold = logIntervalBytes;
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

                if(stream.Length > logIntervalBytes && uploaded >= nextLogThreshold)
                {
                    _logger.LogInformation("Uploading {Path}: {MB:F2} MB of {TotalMB:F2} MB complete",
                        local.RelativePath, uploaded / (1024.0 * 1024.0), stream.Length / (1024.0 * 1024.0));
                    // Send progress update to UI
                    TimeSpan elapsed = fileStopwatch.Elapsed;
                    var bytesPerSecond = elapsed.TotalSeconds > 0 ? uploaded / elapsed.TotalSeconds : 0;
                    TimeSpan? eta = null;
                    if(SyncProgressReporter.ShouldCalculateEta(bytesPerSecond, stream.Length, uploaded))
                    {
                        var remainingBytes = stream.Length - uploaded;
                        var remainingSeconds = bytesPerSecond > 0 ? remainingBytes / bytesPerSecond : 0;
                        eta = TimeSpan.FromSeconds(remainingSeconds);
                    }
                    onProgress?.Invoke(uploaded, elapsed, eta);
                    nextLogThreshold += logIntervalBytes;
                }
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
            _errorLogger.LogError(ex, local.RelativePath);
            log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Failed, Error = ex.Message, BytesTransferred = 0 };
            await _repo.LogTransferAsync(log, cancellationToken);
            throw;
        }
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
            _errorLogger.LogError(ex, local.RelativePath);
            log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Failed, Error = ex.Message, BytesTransferred = 0 };
            await _repo.LogTransferAsync(log, cancellationToken);
        }
    }

    private async Task DownloadItemWithRetryAsync(DriveItemRecord item, CancellationToken cancellationToken, Action<long, TimeSpan, TimeSpan?>? onProgress = null)
    {
        try
        {
            await _retryPolicy.ExecuteAsync(async _ => await DownloadItemAsync(item, cancellationToken, onProgress), cancellationToken);
        }
        catch(Exception ex)
        {
            _errorLogger.LogError(ex, item.RelativePath);
            throw new IOException($"Download failed for {item.RelativePath} after {_settings.UiSettings.SyncSettings.MaxRetries} retries. See inner exception for details.", ex);
        }
    }

    private async Task DownloadItemAsync(DriveItemRecord item, CancellationToken cancellationToken, Action<long, TimeSpan, TimeSpan?>? onProgress = null)
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
                    TimeSpan elapsed = _operationStopwatch.Elapsed;
                    var bytesPerSecond = elapsed.TotalSeconds > 0 ? totalWritten / elapsed.TotalSeconds : 0;
                    TimeSpan? eta = null;
                    if(SyncProgressReporter.ShouldCalculateEta(bytesPerSecond, item.Size, totalWritten))
                    {
                        var remainingBytes = item.Size - totalWritten;
                        var remainingSeconds = bytesPerSecond > 0 ? remainingBytes / bytesPerSecond : 0;
                        eta = TimeSpan.FromSeconds(remainingSeconds);
                    }

                    onProgress?.Invoke(totalWritten, elapsed, eta);
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
            _errorLogger.LogError(ex, item.RelativePath);
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
}
