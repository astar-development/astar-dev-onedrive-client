using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
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
    private readonly SyncSettings _settings;
    private readonly SemaphoreSlim _downloadSemaphore;
    private readonly AsyncRetryPolicy _retryPolicy;

    public TransferService(IFileSystemAdapter fs, IGraphClient graph, ISyncRepository repo, ILogger<TransferService> logger, SyncSettings settings)
    {
        _fs = fs;
        _graph = graph;
        _repo = repo;
        _logger = logger;
        _settings = settings;
        _downloadSemaphore = new SemaphoreSlim(settings.ParallelDownloads);

        _retryPolicy = Policy.Handle<Exception>()
                             .WaitAndRetryAsync(settings.MaxRetries, i => TimeSpan.FromMilliseconds(settings.RetryBaseDelayMs * Math.Pow(2, i)),
                                 (ex, ts, retryCount, ctx) => _logger.LogWarning(ex, "Retry {Retry} after {Delay}ms", retryCount, ts.TotalMilliseconds));
    }

    /// <summary>
    /// Pulls pending downloads from repository in batches and downloads them with bounded concurrency.
    /// </summary>
    public async Task ProcessPendingDownloadsAsync(CancellationToken ct)
    {
        _logger.LogInformation("Processing pending downloads");
        while(!ct.IsCancellationRequested)
        {
            var items = (await _repo.GetPendingDownloadsAsync(_settings.BatchSize, ct)).ToList();
            if(items.Count == 0)
                break;

            var tasks = items.Select(item => DownloadItemWithRetryAsync(item, ct)).ToList();
            await Task.WhenAll(tasks);
        }
    }

    private async Task DownloadItemWithRetryAsync(DriveItemRecord item, CancellationToken ct)
        => await _retryPolicy.ExecuteAsync(async ct2 => await DownloadItemAsync(item, ct2), ct);

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
        var uploads = (await _repo.GetPendingUploadsAsync(_settings.BatchSize, ct)).ToList();
        foreach(LocalFileRecord? local in uploads)
        {
            await _retryPolicy.ExecuteAsync(async ct2 => await UploadLocalFileAsync(local, ct2), ct);
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
}
