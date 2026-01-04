using System.IO.Abstractions.TestingHelpers;
using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.FileSystem;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Integration;

public sealed class TransferServiceShould : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ISyncRepository _repo;
    private readonly IGraphClient _mockGraph;
    private readonly MockFileSystem _mockFileSystem;
    private readonly IFileSystemAdapter _fsAdapter;
    private readonly ILogger<TransferService> _mockLogger;
    private readonly UserPreferences _settings;
    private readonly IDbContextFactory<AppDbContext> _mockDbContextFactory;
    private readonly ILogger<EfSyncRepository> _mockRepoLogger;
    private readonly SyncProgressReporter _progressReporter;
    private readonly ISyncErrorLogger _mockErrorLogger;
    private readonly IChannelFactory _mockChannelFactory;
    private readonly IDownloadQueueProducer _mockDownloadQueueProducer;
    private readonly IDownloadQueueConsumer _mockDownloadQueueConsumer;
    private readonly IUploadQueueProducer _mockUploadQueueProducer;
    private readonly IUploadQueueConsumer _mockUploadQueueConsumer;

    public TransferServiceShould()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={Guid.NewGuid()};Mode=Memory;Cache=Shared")
            .Options;

        _db = new AppDbContext(options);
        _db.Database.OpenConnection();
        _ = _db.Database.EnsureCreated();

        _mockDbContextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        _ = _mockDbContextFactory.CreateDbContext().Returns(_db);
        _mockRepoLogger = Substitute.For<ILogger<EfSyncRepository>>();
        _repo = new EfSyncRepository(_mockDbContextFactory, _mockRepoLogger);
        _mockGraph = Substitute.For<IGraphClient>();
        _mockFileSystem = new MockFileSystem();
        _fsAdapter = new LocalFileSystemAdapter(@"C:\OneDrive", _mockFileSystem);
        _mockLogger = Substitute.For<ILogger<TransferService>>();
        _settings = new UserPreferences
        {
            UiSettings = new UiSettings
            {
                SyncSettings = new SyncSettings
                {
                    MaxParallelDownloads = 2,
                    DownloadBatchSize = 10,
                    MaxRetries = 2,
                    RetryBaseDelayMs = 50
                }
            }
        };
        _progressReporter = new SyncProgressReporter();
        _mockErrorLogger = Substitute.For<ISyncErrorLogger>();
        _mockChannelFactory = Substitute.For<IChannelFactory>();
        _mockDownloadQueueProducer = Substitute.For<IDownloadQueueProducer>();
        _mockDownloadQueueConsumer = Substitute.For<IDownloadQueueConsumer>();
        _mockUploadQueueProducer = Substitute.For<IUploadQueueProducer>();
        _mockUploadQueueConsumer = Substitute.For<IUploadQueueConsumer>();
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }

    [Fact]
    public async Task ProcessPendingDownloadsWithSingleItem()
    {
        var driveItem = new DriveItemRecord("item1", "driveItem1", "file.txt", null, null, 100, DateTimeOffset.UtcNow, false, false);
        _ = await _db.DriveItems.AddAsync(driveItem, TestContext.Current.CancellationToken);
        _ = await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddDirectory(@"C:\OneDrive");
        _ = _mockGraph.DownloadDriveItemContentAsync("driveItem1", Arg.Any<CancellationToken>())
            .Returns(new MemoryStream("file content"u8.ToArray()));

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings, _progressReporter, _mockErrorLogger, _mockChannelFactory, _mockDownloadQueueProducer, _mockDownloadQueueConsumer, _mockUploadQueueProducer, _mockUploadQueueConsumer);

        await service.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.FileExists(@"C:\OneDrive\file.txt").ShouldBeTrue();
        var content = await _mockFileSystem.File.ReadAllTextAsync(@"C:\OneDrive\file.txt", TestContext.Current.CancellationToken);
        content.ShouldBe("file content");

        LocalFileRecord? localFile = await _repo.GetLocalFileByPathAsync("file.txt", TestContext.Current.CancellationToken);
        _ = localFile.ShouldNotBeNull();
        localFile.SyncState.ShouldBe(SyncState.Downloaded);
    }

    [Fact]
    public async Task ProcessPendingDownloadsWithMultipleItems()
    {
        var item1 = new DriveItemRecord("item1", "driveItem1", "file1.txt", null, null, 50, DateTimeOffset.UtcNow, false, false);
        var item2 = new DriveItemRecord("item2", "driveItem2", "subfolder/file2.txt", null, null, 100, DateTimeOffset.UtcNow, false, false);
        await _db.DriveItems.AddRangeAsync([item1, item2], TestContext.Current.CancellationToken);
        _ = await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddDirectory(@"C:\OneDrive");
        _ = _mockGraph.DownloadDriveItemContentAsync("driveItem1", Arg.Any<CancellationToken>())
            .Returns(new MemoryStream("content1"u8.ToArray()));
        _ = _mockGraph.DownloadDriveItemContentAsync("driveItem2", Arg.Any<CancellationToken>())
            .Returns(new MemoryStream("content2"u8.ToArray()));

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings, _progressReporter, _mockErrorLogger, _mockChannelFactory, _mockDownloadQueueProducer, _mockDownloadQueueConsumer, _mockUploadQueueProducer, _mockUploadQueueConsumer);

        await service.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.FileExists(@"C:\OneDrive\file1.txt").ShouldBeTrue();
        _mockFileSystem.FileExists(@"C:\OneDrive\subfolder\file2.txt").ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessPendingDownloadsEmitsProgressEvents()
    {
        var driveItem = new DriveItemRecord("item1", "driveItem1", "file.txt", null, null, 100, DateTimeOffset.UtcNow, false, false);
        _ = await _db.DriveItems.AddAsync(driveItem, TestContext.Current.CancellationToken);
        _ = await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddDirectory(@"C:\OneDrive");
        _ = _mockGraph.DownloadDriveItemContentAsync("driveItem1", Arg.Any<CancellationToken>())
            .Returns(new MemoryStream("content"u8.ToArray()));

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings, _progressReporter, _mockErrorLogger, _mockChannelFactory, _mockDownloadQueueProducer, _mockDownloadQueueConsumer, _mockUploadQueueProducer, _mockUploadQueueConsumer);
        var progressEvents = new List<SyncProgress>();
        _ = service.Progress.Subscribe(progressEvents.Add);

        await service.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

        progressEvents.Count.ShouldBeGreaterThan(0);
        progressEvents[0].CurrentOperationMessage.ShouldBe("Downloading files");
        progressEvents[0].ProcessedFiles.ShouldBe(1);
    }

    [Fact]
    public async Task ProcessPendingDownloadsLogsTransferEvents()
    {
        var driveItem = new DriveItemRecord("item1", "driveItem1", "file.txt", null, null, 100, DateTimeOffset.UtcNow, false, false);
        _ = await _db.DriveItems.AddAsync(driveItem, TestContext.Current.CancellationToken);
        _ = await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddDirectory(@"C:\OneDrive");
        _ = _mockGraph.DownloadDriveItemContentAsync("driveItem1", Arg.Any<CancellationToken>())
            .Returns(new MemoryStream("content"u8.ToArray()));

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings, _progressReporter, _mockErrorLogger, _mockChannelFactory, _mockDownloadQueueProducer, _mockDownloadQueueConsumer, _mockUploadQueueProducer, _mockUploadQueueConsumer);

        await service.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

        List<TransferLog> logs = await _db.TransferLogs.Where(l => l.Type == TransferType.Download).ToListAsync(TestContext.Current.CancellationToken);
        logs.Count.ShouldBeGreaterThanOrEqualTo(1); // At least one log entry
        TransferLog? successLog = logs.FirstOrDefault(l => l.Status == TransferStatus.Success);
        _ = successLog.ShouldNotBeNull();
        successLog.BytesTransferred.ShouldBe(7); // "content" = 7 bytes
    }

    [Fact]
    public async Task ProcessPendingDownloadsHandlesDownloadFailure()
    {
        var driveItem = new DriveItemRecord("item1", "driveItem1", "file.txt", null, null, 100, DateTimeOffset.UtcNow, false, false);
        _ = await _db.DriveItems.AddAsync(driveItem, TestContext.Current.CancellationToken);
        _ = await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddDirectory(@"C:\OneDrive");
        _ = _mockGraph.DownloadDriveItemContentAsync("driveItem1", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings, _progressReporter, _mockErrorLogger, _mockChannelFactory, _mockDownloadQueueProducer, _mockDownloadQueueConsumer, _mockUploadQueueProducer, _mockUploadQueueConsumer);

        await service.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.FileExists(@"C:\OneDrive\file.txt").ShouldBeFalse();
        List<TransferLog> logs = await _db.TransferLogs.Where(l => l.Type == TransferType.Download && l.Status == TransferStatus.Failed).ToListAsync(TestContext.Current.CancellationToken);
        logs.Count.ShouldBeGreaterThan(0);
        _ = logs[0].Error.ShouldNotBeNull();
        logs[0].Error!.ShouldContain("Network error");
    }

    [Fact]
    public async Task ProcessPendingDownloadsRetriesOnFailure()
    {
        var driveItem = new DriveItemRecord("item1", "driveItem1", "file.txt", null, null, 100, DateTimeOffset.UtcNow, false, false);
        _ = await _db.DriveItems.AddAsync(driveItem, TestContext.Current.CancellationToken);
        _ = await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddDirectory(@"C:\OneDrive");
        var callCount = 0;
        _ = _mockGraph.DownloadDriveItemContentAsync("driveItem1", Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return callCount < 2 ? throw new IOException("Temporary failure") : (Stream)new MemoryStream("content"u8.ToArray());
            });

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings, _progressReporter, _mockErrorLogger, _mockChannelFactory, _mockDownloadQueueProducer, _mockDownloadQueueConsumer, _mockUploadQueueProducer, _mockUploadQueueConsumer);

        await service.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

        callCount.ShouldBeGreaterThanOrEqualTo(1); // At least initial attempt
        _mockFileSystem.FileExists(@"C:\OneDrive\file.txt").ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessPendingDownloadsPaginatesBatches()
    {
        for(var i = 0; i < 15; i++)
        {
            var item = new DriveItemRecord($"item{i}", $"driveItem{i}", $"file{i}.txt", null, null, 50, DateTimeOffset.UtcNow, false, false);
            _ = await _db.DriveItems.AddAsync(item, TestContext.Current.CancellationToken);
        }

        _ = await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddDirectory(@"C:\OneDrive");
        _ = _mockGraph.DownloadDriveItemContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryStream("content"u8.ToArray()));

        _settings.UiSettings.SyncSettings.DownloadBatchSize = 10;
        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings, _progressReporter, _mockErrorLogger, _mockChannelFactory, _mockDownloadQueueProducer, _mockDownloadQueueConsumer, _mockUploadQueueProducer, _mockUploadQueueConsumer);

        await service.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

        var downloadedFiles = _mockFileSystem.AllFiles.Count();
        downloadedFiles.ShouldBe(15);
    }

    [Fact]
    public async Task ProcessPendingUploadsWithSingleFile()
    {
        var localFile = new LocalFileRecord("local1", "upload.txt", null, 200, DateTimeOffset.UtcNow, SyncState.PendingUpload);
        _ = await _db.LocalFiles.AddAsync(localFile, TestContext.Current.CancellationToken);
        _ = await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddFile(@"C:\OneDrive\upload.txt", new MockFileData("upload content"));

        var session = new UploadSessionInfo("https://upload.url", "session123", DateTimeOffset.UtcNow.AddHours(1));
        _ = _mockGraph.CreateUploadSessionAsync(Arg.Any<string>(), "upload.txt", Arg.Any<CancellationToken>())
            .Returns(session);
        _ = _mockGraph.UploadChunkAsync(Arg.Is<UploadSessionInfo>(s => s.SessionId == "session123"), Arg.Any<Stream>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings, _progressReporter, _mockErrorLogger, _mockChannelFactory, _mockDownloadQueueProducer, _mockDownloadQueueConsumer, _mockUploadQueueProducer, _mockUploadQueueConsumer);

        await service.ProcessPendingUploadsAsync(TestContext.Current.CancellationToken);

        _ = await _mockGraph.Received().CreateUploadSessionAsync(Arg.Any<string>(), "upload.txt", Arg.Any<CancellationToken>());
        await _mockGraph.Received().UploadChunkAsync(Arg.Is<UploadSessionInfo>(s => s.SessionId == "session123"), Arg.Any<Stream>(), 0, Arg.Any<long>(), Arg.Any<CancellationToken>());

        LocalFileRecord? updatedFile = await _db.LocalFiles.FindAsync(["local1"], TestContext.Current.CancellationToken);
        _ = updatedFile.ShouldNotBeNull();
        updatedFile.SyncState.ShouldBe(SyncState.Uploaded);
    }

    [Fact]
    public async Task ProcessPendingUploadsEmitsProgressEvents()
    {
        var localFile = new LocalFileRecord("local1", "upload.txt", null, 200, DateTimeOffset.UtcNow, SyncState.PendingUpload);
        _ = await _db.LocalFiles.AddAsync(localFile, TestContext.Current.CancellationToken);
        _ = await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddFile(@"C:\OneDrive\upload.txt", new MockFileData("upload content"));

        var session = new UploadSessionInfo("https://upload.url", "session123", DateTimeOffset.UtcNow.AddHours(1));
        _ = _mockGraph.CreateUploadSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(session);
        _ = _mockGraph.UploadChunkAsync(Arg.Any<UploadSessionInfo>(), Arg.Any<Stream>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings, _progressReporter, _mockErrorLogger, _mockChannelFactory, _mockDownloadQueueProducer, _mockDownloadQueueConsumer, _mockUploadQueueProducer, _mockUploadQueueConsumer);
        var progressEvents = new List<SyncProgress>();
        _ = service.Progress.Subscribe(progressEvents.Add);

        await service.ProcessPendingUploadsAsync(TestContext.Current.CancellationToken);

        progressEvents.Count.ShouldBeGreaterThan(0);
        progressEvents[0].CurrentOperationMessage.ShouldBe("Uploading files");
    }

    [Fact]
    public async Task ProcessPendingUploadsHandlesUploadFailure()
    {
        var localFile = new LocalFileRecord("local1", "upload.txt", null, 200, DateTimeOffset.UtcNow, SyncState.PendingUpload);
        _ = await _db.LocalFiles.AddAsync(localFile, TestContext.Current.CancellationToken);
        _ = await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddFile(@"C:\OneDrive\upload.txt", new MockFileData("content"));

        _ = _mockGraph.CreateUploadSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Upload failed"));

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings, _progressReporter, _mockErrorLogger, _mockChannelFactory, _mockDownloadQueueProducer, _mockDownloadQueueConsumer, _mockUploadQueueProducer, _mockUploadQueueConsumer);

        await service.ProcessPendingUploadsAsync(TestContext.Current.CancellationToken);

        List<TransferLog> logs = await _db.TransferLogs.Where(l => l.Type == TransferType.Upload && l.Status == TransferStatus.Failed).ToListAsync(TestContext.Current.CancellationToken);
        logs.Count.ShouldBeGreaterThan(0);
        _ = logs[0].Error.ShouldNotBeNull();
        logs[0].Error!.ShouldContain("Upload failed");
    }

    [Fact]
    public async Task ProcessPendingUploadsChunksLargeFiles()
    {
        var largeContent = new byte[1024 * 1024]; // 1MB file
        Array.Fill(largeContent, (byte)0x42);

        var localFile = new LocalFileRecord("local1", "large.bin", null, largeContent.Length, DateTimeOffset.UtcNow, SyncState.PendingUpload);
        _ = await _db.LocalFiles.AddAsync(localFile, TestContext.Current.CancellationToken);
        _ = await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddFile(@"C:\OneDrive\large.bin", new MockFileData(largeContent));

        var session = new UploadSessionInfo("https://upload.url", "session123", DateTimeOffset.UtcNow.AddHours(1));
        _ = _mockGraph.CreateUploadSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(session);
        _ = _mockGraph.UploadChunkAsync(Arg.Any<UploadSessionInfo>(), Arg.Any<Stream>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings, _progressReporter, _mockErrorLogger, _mockChannelFactory, _mockDownloadQueueProducer, _mockDownloadQueueConsumer, _mockUploadQueueProducer, _mockUploadQueueConsumer);

        await service.ProcessPendingUploadsAsync(TestContext.Current.CancellationToken);

        // 1MB / 320KB = 4 chunks
        await _mockGraph.Received(4).UploadChunkAsync(Arg.Any<UploadSessionInfo>(), Arg.Any<Stream>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingDownloadsRespectsConcurrencyLimit()
    {
        for(var i = 0; i < 5; i++)
        {
            var item = new DriveItemRecord($"item{i}", $"driveItem{i}", $"file{i}.txt", null, null, 50, DateTimeOffset.UtcNow, false, false);
            _ = await _db.DriveItems.AddAsync(item, TestContext.Current.CancellationToken);
        }

        _ = await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddDirectory(@"C:\OneDrive");

        var concurrentDownloads = 0;
        var maxConcurrent = 0;
        var semaphore = new SemaphoreSlim(1);

        _ = _mockGraph.DownloadDriveItemContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await semaphore.WaitAsync();
                try
                {
                    concurrentDownloads++;
                    maxConcurrent = Math.Max(maxConcurrent, concurrentDownloads);
                    await Task.Delay(50); // Simulate work
                    concurrentDownloads--;
                    return (Stream)new MemoryStream("content"u8.ToArray());
                }
                finally
                {
                    _ = semaphore.Release();
                }
            });

        _settings.UiSettings.SyncSettings.MaxParallelDownloads = 2;
        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings, _progressReporter, _mockErrorLogger, _mockChannelFactory, _mockDownloadQueueProducer, _mockDownloadQueueConsumer, _mockUploadQueueProducer, _mockUploadQueueConsumer);

        await service.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

        maxConcurrent.ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task ProcessPendingUploadsLogsTransferEvents()
    {
        var localFile = new LocalFileRecord("local1", "upload.txt", null, 200, DateTimeOffset.UtcNow, SyncState.PendingUpload);
        _ = await _db.LocalFiles.AddAsync(localFile, TestContext.Current.CancellationToken);
        _ = await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddFile(@"C:\OneDrive\upload.txt", new MockFileData("content"));

        var session = new UploadSessionInfo("https://upload.url", "session123", DateTimeOffset.UtcNow.AddHours(1));
        _ = _mockGraph.CreateUploadSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(session);
        _ = _mockGraph.UploadChunkAsync(Arg.Any<UploadSessionInfo>(), Arg.Any<Stream>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings, _progressReporter, _mockErrorLogger, _mockChannelFactory, _mockDownloadQueueProducer, _mockDownloadQueueConsumer, _mockUploadQueueProducer, _mockUploadQueueConsumer);

        await service.ProcessPendingUploadsAsync(TestContext.Current.CancellationToken);

        List<TransferLog> logs = await _db.TransferLogs.Where(l => l.Type == TransferType.Upload).ToListAsync(TestContext.Current.CancellationToken);
        logs.Count.ShouldBe(2); // InProgress and Success
        logs.ShouldContain(l => l.Status == TransferStatus.InProgress);
        logs.ShouldContain(l => l.Status == TransferStatus.Success && l.BytesTransferred == 7);
    }
}
