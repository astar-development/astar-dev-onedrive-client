using System.Reactive.Subjects;
using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Models;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace AStar.Dev.OneDrive.Client.Services;

public sealed class TransferService
{
    private readonly IFileSystemAdapter _fs;
    private readonly IGraphClient _graph;
    private readonly ISyncRepository _repo;
    private readonly ILogger<TransferService> _logger;
    private readonly UserPreferences _settings;
    private readonly SemaphoreSlim _downloadSemaphore;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly Subject<SyncProgress> _progressSubject = new();

    public IObservable<SyncProgress> Progress => _progressSubject;

    public TransferService(IFileSystemAdapter fs, IGraphClient graph, ISyncRepository repo, ILogger<TransferService> logger, UserPreferences settings)
    {
        _fs = fs;
        _graph = graph;
        _repo = repo;
        _logger = logger;
        _settings = settings;
        _downloadSemaphore = new SemaphoreSlim(settings.UiSettings.SyncSettings.MaxParallelDownloads);

        _retryPolicy = Policy.Handle<Exception>()
                             .WaitAndRetryAsync(settings.UiSettings.SyncSettings.MaxRetries, i => TimeSpan.FromMilliseconds(settings.UiSettings.SyncSettings.RetryBaseDelayMs * Math.Pow(2, i)),
                                 (ex, ts, retryCount, ctx) => _logger.LogWarning(ex, "Retry {Retry} after {Delay}ms", retryCount, ts.TotalMilliseconds));
    }

    /// <summary>
    /// Pulls pending downloads from repository in batches and downloads them with bounded concurrency.
    /// </summary>
    public async Task ProcessPendingDownloadsAsync(CancellationToken ct)
    {
        _logger.LogInformation("Processing pending downloads");
        var totalProcessed = 0;
        var pageCount = 0;
        var batchSize = _settings.UiSettings.SyncSettings.DownloadBatchSize>0? _settings.UiSettings.SyncSettings.DownloadBatchSize : 100;
        var total = await _repo.GetPendingDownloadCountAsync(ct);
        while(!ct.IsCancellationRequested)
        {
            var items = (await _repo.GetPendingDownloadsAsync(batchSize, pageCount++, ct)).ToList();
            if(items.Count == 0)
                break;

            var tasks = items.Select(item => DownloadItemWithRetryAsync(item, ct, () =>
            {
                totalProcessed++;
                ReportProgress(totalProcessed, "Downloading files", total);
            })).ToList();
            await Task.WhenAll(tasks);
        }
    }

    private async Task DownloadItemWithRetryAsync(DriveItemRecord item, CancellationToken ct, Action? onComplete = null) => await _retryPolicy.ExecuteAsync(async ct2 =>
                                                                                                                                 {
                                                                                                                                     await DownloadItemAsync(item, ct2);
                                                                                                                                     onComplete?.Invoke();
                                                                                                                                 }, ct);

    private async Task DownloadItemAsync(DriveItemRecord item, CancellationToken ct)
    {
        await _downloadSemaphore.WaitAsync(ct);
        var log = new TransferLog(Guid.NewGuid().ToString(), TransferType.Download, item.Id, DateTimeOffset.UtcNow, null, TransferStatus.InProgress, item.Size, null);
        await _repo.LogTransferAsync(log, ct);
        try
        {
            await using Stream stream = await _graph.DownloadDriveItemContentAsync(item.DriveItemId, ct);
            await _fs.WriteFileAsync(item.RelativePath, stream, ct);
            await _repo.MarkLocalFileStateAsync(item.Id, SyncState.Downloaded, ct);
            FileInfo fileInfo = _fs.GetFileInfo(item.RelativePath);
            log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Success, BytesTransferred = fileInfo.Length };
            await _repo.LogTransferAsync(log, ct);
            _logger.LogInformation("Downloaded {Path}", item.RelativePath);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Download failed for {Id}", item.Id);
            log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Failed, Error = ex.Message, BytesTransferred = 0 };
            await _repo.LogTransferAsync(log, ct);
        }
        finally
        {
            _ = _downloadSemaphore.Release();
        }
    }

    /// <summary>
    /// Scans repository for pending uploads and uploads them using upload sessions and chunked uploads.
    /// </summary>
    public async Task ProcessPendingUploadsAsync(CancellationToken ct)
    {
        _logger.LogInformation("Processing pending uploads");
        var uploads = (await _repo.GetPendingUploadsAsync(_settings.UiSettings.SyncSettings.DownloadBatchSize, ct)).ToList();
        var totalProcessed = 0;
        var pendingUploads = uploads.Count;
        foreach(LocalFileRecord? local in uploads)
        {
            await _retryPolicy.ExecuteAsync(async ct2 =>
            {
                await UploadLocalFileAsync(local, ct2);
                totalProcessed++;
                ReportProgress(totalProcessed, "Uploading files", uploads.Count, pendingUploads);
            }, ct);
        }
    }

    private async Task UploadLocalFileAsync(LocalFileRecord local, CancellationToken ct)
    {
        var log = new TransferLog(Guid.NewGuid().ToString(), TransferType.Upload, local.Id, DateTimeOffset.UtcNow, null, TransferStatus.InProgress, 0, null);
        await _repo.LogTransferAsync(log, ct);
        try
        {
            var parent = Path.GetDirectoryName(local.RelativePath) ?? "/";
            var fileName = Path.GetFileName(local.RelativePath);
            UploadSessionInfo session = await _graph.CreateUploadSessionAsync(parent, fileName, ct);

            await using Stream stream = await _fs.OpenReadAsync(local.RelativePath, ct) ?? throw new FileNotFoundException(local.RelativePath);
            const int chunkSize = 320 * 1024; // 320KB
            long uploaded = 0;
            while(uploaded < stream.Length)
            {
                var toRead = (int)Math.Min(chunkSize, stream.Length - uploaded);
                var buffer = new byte[toRead];
                _ = stream.Seek(uploaded, SeekOrigin.Begin);
                var read = await stream.ReadAsync(buffer.AsMemory(0, toRead), ct);
                await using var ms = new MemoryStream(buffer, 0, read, writable: false);
                await _graph.UploadChunkAsync(session, ms, uploaded, uploaded + read - 1, ct);
                uploaded += read;
            }

            await _repo.MarkLocalFileStateAsync(local.Id, SyncState.Uploaded, ct);
            log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Success, BytesTransferred = stream.Length };
            await _repo.LogTransferAsync(log, ct);
            _logger.LogInformation("Uploaded {Path}", local.RelativePath);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Upload failed for {Id}", local.Id);
            log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Failed, Error = ex.Message, BytesTransferred = 0 };
            await _repo.LogTransferAsync(log, ct);
        }
    }

    private void ReportProgress(int processed, string operation, int total = 0, int pendingUploads = 0)
    {
        var syncProgress = new SyncProgress
        {
            CurrentOperation = operation,
            ProcessedFiles = processed,
            TotalFiles = total,
            PendingDownloads = total - processed,
            PendingUploads = pendingUploads
        };

        _progressSubject.OnNext(syncProgress);
    }
}
