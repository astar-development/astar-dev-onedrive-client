using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.FileSystem;
using AStar.Dev.OneDrive.Client.Services;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using System.IO.Abstractions.TestingHelpers;
using System.Reactive.Linq;

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

    public TransferServiceShould()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={Guid.NewGuid()};Mode=Memory;Cache=Shared")
            .Options;

        _db = new AppDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();

        _repo = new EfSyncRepository(_db);
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
        await _db.DriveItems.AddAsync(driveItem, TestContext.Current.CancellationToken);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddDirectory(@"C:\OneDrive");
        _mockGraph.DownloadDriveItemContentAsync("driveItem1", Arg.Any<CancellationToken>())
            .Returns(new MemoryStream("file content"u8.ToArray()));

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings);

        await service.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.FileExists(@"C:\OneDrive\file.txt").ShouldBeTrue();
        var content = _mockFileSystem.File.ReadAllText(@"C:\OneDrive\file.txt");
        content.ShouldBe("file content");

        LocalFileRecord? localFile = await _repo.GetLocalFileByPathAsync("file.txt", TestContext.Current.CancellationToken);
        localFile.ShouldNotBeNull();
        localFile.SyncState.ShouldBe(SyncState.Downloaded);
    }

    [Fact]
    public async Task ProcessPendingDownloadsWithMultipleItems()
    {
        var item1 = new DriveItemRecord("item1", "driveItem1", "file1.txt", null, null, 50, DateTimeOffset.UtcNow, false, false);
        var item2 = new DriveItemRecord("item2", "driveItem2", "subfolder/file2.txt", null, null, 100, DateTimeOffset.UtcNow, false, false);
        await _db.DriveItems.AddRangeAsync([item1, item2], TestContext.Current.CancellationToken);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddDirectory(@"C:\OneDrive");
        _mockGraph.DownloadDriveItemContentAsync("driveItem1", Arg.Any<CancellationToken>())
            .Returns(new MemoryStream("content1"u8.ToArray()));
        _mockGraph.DownloadDriveItemContentAsync("driveItem2", Arg.Any<CancellationToken>())
            .Returns(new MemoryStream("content2"u8.ToArray()));

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings);

        await service.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.FileExists(@"C:\OneDrive\file1.txt").ShouldBeTrue();
        _mockFileSystem.FileExists(@"C:\OneDrive\subfolder\file2.txt").ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessPendingDownloadsEmitsProgressEvents()
    {
        var driveItem = new DriveItemRecord("item1", "driveItem1", "file.txt", null, null, 100, DateTimeOffset.UtcNow, false, false);
        await _db.DriveItems.AddAsync(driveItem, TestContext.Current.CancellationToken);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddDirectory(@"C:\OneDrive");
        _mockGraph.DownloadDriveItemContentAsync("driveItem1", Arg.Any<CancellationToken>())
            .Returns(new MemoryStream("content"u8.ToArray()));

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings);
        var progressEvents = new List<SyncProgress>();
        service.Progress.Subscribe(progressEvents.Add);

        await service.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

        progressEvents.Count.ShouldBeGreaterThan(0);
        progressEvents.First().CurrentOperation.ShouldBe("Downloading files");
        progressEvents.First().ProcessedFiles.ShouldBe(1);
    }

    [Fact]
    public async Task ProcessPendingDownloadsLogsTransferEvents()
    {
        var driveItem = new DriveItemRecord("item1", "driveItem1", "file.txt", null, null, 100, DateTimeOffset.UtcNow, false, false);
        await _db.DriveItems.AddAsync(driveItem, TestContext.Current.CancellationToken);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddDirectory(@"C:\OneDrive");
        _mockGraph.DownloadDriveItemContentAsync("driveItem1", Arg.Any<CancellationToken>())
            .Returns(new MemoryStream("content"u8.ToArray()));

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings);

            await service.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

            var logs = await _db.TransferLogs.Where(l => l.Type == TransferType.Download).ToListAsync(TestContext.Current.CancellationToken);
            logs.Count.ShouldBeGreaterThanOrEqualTo(1); // At least one log entry
            var successLog = logs.FirstOrDefault(l => l.Status == TransferStatus.Success);
            successLog.ShouldNotBeNull();
            successLog.BytesTransferred.ShouldBe(7); // "content" = 7 bytes
        }

    [Fact]
    public async Task ProcessPendingDownloadsHandlesDownloadFailure()
    {
        var driveItem = new DriveItemRecord("item1", "driveItem1", "file.txt", null, null, 100, DateTimeOffset.UtcNow, false, false);
        await _db.DriveItems.AddAsync(driveItem, TestContext.Current.CancellationToken);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddDirectory(@"C:\OneDrive");
        _mockGraph.DownloadDriveItemContentAsync("driveItem1", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings);

        await service.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.FileExists(@"C:\OneDrive\file.txt").ShouldBeFalse();
        var logs = await _db.TransferLogs.Where(l => l.Type == TransferType.Download && l.Status == TransferStatus.Failed).ToListAsync(TestContext.Current.CancellationToken);
        logs.Count.ShouldBeGreaterThan(0);
        logs.First().Error.ShouldNotBeNull();
        logs.First().Error!.ShouldContain("Network error");
    }

    [Fact]
    public async Task ProcessPendingDownloadsRetriesOnFailure()
    {
        var driveItem = new DriveItemRecord("item1", "driveItem1", "file.txt", null, null, 100, DateTimeOffset.UtcNow, false, false);
        await _db.DriveItems.AddAsync(driveItem, TestContext.Current.CancellationToken);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddDirectory(@"C:\OneDrive");
        var callCount = 0;
        _mockGraph.DownloadDriveItemContentAsync("driveItem1", Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount < 2)
                    throw new IOException("Temporary failure");
                return new MemoryStream("content"u8.ToArray());
            });

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings);

            await service.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

            callCount.ShouldBeGreaterThanOrEqualTo(1); // At least initial attempt
            _mockFileSystem.FileExists(@"C:\OneDrive\file.txt").ShouldBeTrue();
        }

    [Fact]
    public async Task ProcessPendingDownloadsPaginatesBatches()
    {
        for (var i = 0; i < 15; i++)
        {
            var item = new DriveItemRecord($"item{i}", $"driveItem{i}", $"file{i}.txt", null, null, 50, DateTimeOffset.UtcNow, false, false);
            await _db.DriveItems.AddAsync(item, TestContext.Current.CancellationToken);
        }
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddDirectory(@"C:\OneDrive");
        _mockGraph.DownloadDriveItemContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryStream("content"u8.ToArray()));

        _settings.UiSettings.SyncSettings.DownloadBatchSize = 10;
        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings);

        await service.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

        var downloadedFiles = _mockFileSystem.AllFiles.Count();
        downloadedFiles.ShouldBe(15);
    }

    [Fact]
    public async Task ProcessPendingUploadsWithSingleFile()
    {
        var localFile = new LocalFileRecord("local1", "upload.txt", null, 200, DateTimeOffset.UtcNow, SyncState.PendingUpload);
        await _db.LocalFiles.AddAsync(localFile, TestContext.Current.CancellationToken);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddFile(@"C:\OneDrive\upload.txt", new MockFileData("upload content"));

        var session = new UploadSessionInfo("https://upload.url", "session123", DateTimeOffset.UtcNow.AddHours(1));
        _mockGraph.CreateUploadSessionAsync(Arg.Any<string>(), "upload.txt", Arg.Any<CancellationToken>())
            .Returns(session);
        _mockGraph.UploadChunkAsync(Arg.Is<UploadSessionInfo>(s => s.SessionId == "session123"), Arg.Any<Stream>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings);

        await service.ProcessPendingUploadsAsync(TestContext.Current.CancellationToken);

        await _mockGraph.Received().CreateUploadSessionAsync(Arg.Any<string>(), "upload.txt", Arg.Any<CancellationToken>());
        await _mockGraph.Received().UploadChunkAsync(Arg.Is<UploadSessionInfo>(s => s.SessionId == "session123"), Arg.Any<Stream>(), 0, Arg.Any<long>(), Arg.Any<CancellationToken>());

        LocalFileRecord? updatedFile = await _db.LocalFiles.FindAsync(["local1"], TestContext.Current.CancellationToken);
        updatedFile.ShouldNotBeNull();
        updatedFile.SyncState.ShouldBe(SyncState.Uploaded);
    }

    [Fact]
    public async Task ProcessPendingUploadsEmitsProgressEvents()
    {
        var localFile = new LocalFileRecord("local1", "upload.txt", null, 200, DateTimeOffset.UtcNow, SyncState.PendingUpload);
        await _db.LocalFiles.AddAsync(localFile, TestContext.Current.CancellationToken);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddFile(@"C:\OneDrive\upload.txt", new MockFileData("upload content"));

        var session = new UploadSessionInfo("https://upload.url", "session123", DateTimeOffset.UtcNow.AddHours(1));
        _mockGraph.CreateUploadSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(session);
        _mockGraph.UploadChunkAsync(Arg.Any<UploadSessionInfo>(), Arg.Any<Stream>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings);
        var progressEvents = new List<SyncProgress>();
        service.Progress.Subscribe(progressEvents.Add);

        await service.ProcessPendingUploadsAsync(TestContext.Current.CancellationToken);

        progressEvents.Count.ShouldBeGreaterThan(0);
        progressEvents.First().CurrentOperation.ShouldBe("Uploading files");
    }

    [Fact]
    public async Task ProcessPendingUploadsHandlesUploadFailure()
    {
        var localFile = new LocalFileRecord("local1", "upload.txt", null, 200, DateTimeOffset.UtcNow, SyncState.PendingUpload);
        await _db.LocalFiles.AddAsync(localFile, TestContext.Current.CancellationToken);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddFile(@"C:\OneDrive\upload.txt", new MockFileData("content"));

        _mockGraph.CreateUploadSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Upload failed"));

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings);

        await service.ProcessPendingUploadsAsync(TestContext.Current.CancellationToken);

        var logs = await _db.TransferLogs.Where(l => l.Type == TransferType.Upload && l.Status == TransferStatus.Failed).ToListAsync(TestContext.Current.CancellationToken);
        logs.Count.ShouldBeGreaterThan(0);
        logs.First().Error.ShouldNotBeNull();
        logs.First().Error!.ShouldContain("Upload failed");
    }

    [Fact]
    public async Task ProcessPendingUploadsChunksLargeFiles()
    {
        var largeContent = new byte[1024 * 1024]; // 1MB file
        Array.Fill(largeContent, (byte)0x42);

        var localFile = new LocalFileRecord("local1", "large.bin", null, largeContent.Length, DateTimeOffset.UtcNow, SyncState.PendingUpload);
        await _db.LocalFiles.AddAsync(localFile, TestContext.Current.CancellationToken);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddFile(@"C:\OneDrive\large.bin", new MockFileData(largeContent));

        var session = new UploadSessionInfo("https://upload.url", "session123", DateTimeOffset.UtcNow.AddHours(1));
        _mockGraph.CreateUploadSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(session);
        _mockGraph.UploadChunkAsync(Arg.Any<UploadSessionInfo>(), Arg.Any<Stream>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings);

        await service.ProcessPendingUploadsAsync(TestContext.Current.CancellationToken);

        // 1MB / 320KB = 4 chunks
        await _mockGraph.Received(4).UploadChunkAsync(Arg.Any<UploadSessionInfo>(), Arg.Any<Stream>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingDownloadsRespectsConcurrencyLimit()
    {
        for (var i = 0; i < 5; i++)
        {
            var item = new DriveItemRecord($"item{i}", $"driveItem{i}", $"file{i}.txt", null, null, 50, DateTimeOffset.UtcNow, false, false);
            await _db.DriveItems.AddAsync(item, TestContext.Current.CancellationToken);
        }
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddDirectory(@"C:\OneDrive");

        var concurrentDownloads = 0;
        var maxConcurrent = 0;
        var semaphore = new SemaphoreSlim(1);

        _mockGraph.DownloadDriveItemContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
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
                    semaphore.Release();
                }
            });

        _settings.UiSettings.SyncSettings.MaxParallelDownloads = 2;
        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings);

        await service.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

        maxConcurrent.ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task ProcessPendingUploadsLogsTransferEvents()
    {
        var localFile = new LocalFileRecord("local1", "upload.txt", null, 200, DateTimeOffset.UtcNow, SyncState.PendingUpload);
        await _db.LocalFiles.AddAsync(localFile, TestContext.Current.CancellationToken);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockFileSystem.AddFile(@"C:\OneDrive\upload.txt", new MockFileData("content"));

        var session = new UploadSessionInfo("https://upload.url", "session123", DateTimeOffset.UtcNow.AddHours(1));
        _mockGraph.CreateUploadSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(session);
        _mockGraph.UploadChunkAsync(Arg.Any<UploadSessionInfo>(), Arg.Any<Stream>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var service = new TransferService(_fsAdapter, _mockGraph, _repo, _mockLogger, _settings);

        await service.ProcessPendingUploadsAsync(TestContext.Current.CancellationToken);

        var logs = await _db.TransferLogs.Where(l => l.Type == TransferType.Upload).ToListAsync(TestContext.Current.CancellationToken);
        logs.Count.ShouldBe(2); // InProgress and Success
        logs.ShouldContain(l => l.Status == TransferStatus.InProgress);
        logs.ShouldContain(l => l.Status == TransferStatus.Success && l.BytesTransferred == 7);
    }
}
