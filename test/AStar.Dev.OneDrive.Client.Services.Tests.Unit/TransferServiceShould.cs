using System.IO.Abstractions.TestingHelpers;
using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit;

public class TransferServiceShould
{
    private static TransferService CreateSut(
        IFileSystemAdapter? fs = null,
        IGraphClient? graph = null,
        ISyncRepository? repo = null,
        ILogger<TransferService>? logger = null,
        UserPreferences? settings = null)
    {
        fs ??= Substitute.For<IFileSystemAdapter>();
        graph ??= Substitute.For<IGraphClient>();
        repo ??= Substitute.For<ISyncRepository>();
        logger ??= Substitute.For<ILogger<TransferService>>();
        settings ??= new UserPreferences
        {
            UiSettings = new UiSettings
            {
                SyncSettings = new SyncSettings
                {
                    MaxParallelDownloads = 2,
                    DownloadBatchSize = 1,
                    MaxRetries = 1,
                    RetryBaseDelayMs = 10
                }
            }
        };
        return new TransferService(fs, graph, repo, logger, settings);
    }

    [Fact]
    public async Task NotReportProgressWhenNoPendingDownloads()
    {
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(0);
        TransferService sut = CreateSut(repo: repo);
        var progressReported = false;
        using IDisposable subscription = sut.Progress.Subscribe(_ => progressReported = true);

        await sut.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

        progressReported.ShouldBeFalse(); // No progress should be reported
    }

    [Fact]
    public async Task LogWarningWhenDownloadIsCancelled()
    {
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(1);
        repo.GetPendingDownloadsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        ILogger<TransferService> logger = Substitute.For<ILogger<TransferService>>();
        TransferService sut = CreateSut(repo: repo, logger: logger);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await sut.ProcessPendingDownloadsAsync(cts.Token);

        logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ThrowWhenDownloadItemFailsAfterRetries()
    {
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(1);
        repo.GetPendingDownloadsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([
            new DriveItemRecord("id", "did", "file.txt", null, null, 100, DateTimeOffset.UtcNow, false, false)
        ]);
        IFileSystemAdapter fs = Substitute.For<IFileSystemAdapter>();
        IGraphClient graph = Substitute.For<IGraphClient>();
        graph.DownloadDriveItemContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<System.IO.Stream>(new IOException("fail")));
        ILogger<TransferService> logger = Substitute.For<ILogger<TransferService>>();
        UserPreferences settings = new()
        {
            UiSettings = new UiSettings
            {
                SyncSettings = new SyncSettings
                {
                    MaxParallelDownloads = 2,
                    DownloadBatchSize = 1,
                    MaxRetries = 1, // Only retry once
                    RetryBaseDelayMs = 1 // 1 ms delay
                }
            }
        };
        TransferService sut = CreateSut(fs: fs, graph: graph, repo: repo, logger: logger, settings: settings);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        Exception? ex = await Record.ExceptionAsync(() => sut.ProcessPendingDownloadsAsync(cts.Token));
        if(ex is AggregateException aggEx)
        {
            aggEx = aggEx.Flatten();
            aggEx.InnerExceptions.ShouldContain(e => e is IOException);
        }
        else
        {
            ex.ShouldBeOfType<IOException>();
        }
    }

    [Fact]
    public async Task ReportProgressDuringDownloads()
    {
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(2);
        // Only return items for the first call, then return empty for subsequent calls
        var callCount = 0;
        repo.GetPendingDownloadsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ => callCount++ == 0
                    ? [
                new("id1", "did1", "file1.txt", null, null, 100, DateTimeOffset.UtcNow, false, false),
                new("id2", "did2", "file2.txt", null, null, 200, DateTimeOffset.UtcNow, false, false)
                    ]
                    : []);
        IGraphClient graph = Substitute.For<IGraphClient>();
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddFile("file1.txt", new MockFileData("dummy"));
        mockFileSystem.AddFile("file2.txt", new MockFileData("dummy"));
        IFileSystemAdapter fs = Substitute.For<IFileSystemAdapter>();
        fs.GetFileInfo(Arg.Any<string>()).Returns(callInfo => mockFileSystem.FileInfo.New((string)callInfo[0]));
        fs.WriteFileAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        fs.OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Stream?>(new MemoryStream(new byte[10])));
        repo.MarkLocalFileStateAsync(Arg.Any<string>(), Arg.Any<SyncState>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        repo.LogTransferAsync(Arg.Any<TransferLog>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        graph.DownloadDriveItemContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Stream>(new MemoryStream(new byte[10])));
        ILogger<TransferService> logger = Substitute.For<ILogger<TransferService>>();
        UserPreferences settings = new()
        {
            UiSettings = new UiSettings
            {
                SyncSettings = new SyncSettings
                {
                    MaxParallelDownloads = 2,
                    DownloadBatchSize = 2,
                    MaxRetries = 1,
                    RetryBaseDelayMs = 1
                }
            }
        };
        TransferService sut = CreateSut(fs: fs, graph: graph, repo: repo, logger: logger, settings: settings);

        var progressCount = 0;
        using IDisposable subscription = sut.Progress.Subscribe(_ => progressCount++);

        await sut.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

        progressCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ReportProgressDuringUploads()
    {
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        repo.GetPendingUploadsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([
            new LocalFileRecord("id1", "file1.txt", "hash1", 100, DateTimeOffset.UtcNow, SyncState.PendingUpload),
        new LocalFileRecord("id2", "file2.txt", "hash2", 200, DateTimeOffset.UtcNow, SyncState.PendingUpload)
        ]);
        repo.LogTransferAsync(Arg.Any<TransferLog>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        repo.MarkLocalFileStateAsync(Arg.Any<string>(), Arg.Any<SyncState>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddFile("file1.txt", new MockFileData("dummy"));
        mockFileSystem.AddFile("file2.txt", new MockFileData("dummy"));
        IFileSystemAdapter fs = Substitute.For<IFileSystemAdapter>();
        fs.OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Stream?>(new MemoryStream(new byte[10])));
        fs.GetFileInfo(Arg.Any<string>()).Returns(callInfo => mockFileSystem.FileInfo.New((string)callInfo[0]));
        IGraphClient graph = Substitute.For<IGraphClient>();
        graph.CreateUploadSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new UploadSessionInfo("url", "id", DateTimeOffset.UtcNow.AddMinutes(5)));
        graph.UploadChunkAsync(Arg.Any<UploadSessionInfo>(), Arg.Any<Stream>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        ILogger<TransferService> logger = Substitute.For<ILogger<TransferService>>();
        UserPreferences settings = new()
        {
            UiSettings = new UiSettings
            {
                SyncSettings = new SyncSettings
                {
                    MaxParallelDownloads = 2,
                    DownloadBatchSize = 2,
                    MaxRetries = 1,
                    RetryBaseDelayMs = 1
                }
            }
        };
        TransferService sut = CreateSut(fs: fs, graph: graph, repo: repo, logger: logger, settings: settings);

        var progressCount = 0;
        using IDisposable subscription = sut.Progress.Subscribe(_ => progressCount++);

        await sut.ProcessPendingUploadsAsync(TestContext.Current.CancellationToken);

        progressCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task MarkUploadAsFailedWhenExceptionThrown()
    {
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        repo.GetPendingUploadsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([
            new LocalFileRecord("id1", "file1.txt", "hash1", 100, DateTimeOffset.UtcNow, SyncState.PendingUpload)
        ]);
        repo.LogTransferAsync(Arg.Any<TransferLog>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        repo.MarkLocalFileStateAsync(Arg.Any<string>(), Arg.Any<SyncState>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddFile("file1.txt", new MockFileData("dummy"));
        IFileSystemAdapter fs = Substitute.For<IFileSystemAdapter>();
        fs.OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<Stream?>(new IOException("fail")));
        fs.GetFileInfo(Arg.Any<string>()).Returns(callInfo => mockFileSystem.FileInfo.New((string)callInfo[0]));
        IGraphClient graph = Substitute.For<IGraphClient>();
        graph.CreateUploadSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new UploadSessionInfo("url", "id", DateTimeOffset.UtcNow.AddMinutes(5)));
        graph.UploadChunkAsync(Arg.Any<UploadSessionInfo>(), Arg.Any<Stream>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        ILogger<TransferService> logger = Substitute.For<ILogger<TransferService>>();
        UserPreferences settings = new()
        {
            UiSettings = new UiSettings
            {
                SyncSettings = new SyncSettings
                {
                    MaxParallelDownloads = 2,
                    DownloadBatchSize = 1,
                    MaxRetries = 1,
                    RetryBaseDelayMs = 1
                }
            }
        };
        TransferService sut = CreateSut(fs: fs, graph: graph, repo: repo, logger: logger, settings: settings);

        await sut.ProcessPendingUploadsAsync(TestContext.Current.CancellationToken);

        await repo.Received().LogTransferAsync(Arg.Is<TransferLog>(t => t.Status == TransferStatus.Failed), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelUploadsShouldLogWarning()
    {
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        repo.GetPendingUploadsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([
            new LocalFileRecord("id1", "file1.txt", "hash1", 100, DateTimeOffset.UtcNow, SyncState.PendingUpload)
        ]);
        repo.LogTransferAsync(Arg.Any<TransferLog>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        repo.MarkLocalFileStateAsync(Arg.Any<string>(), Arg.Any<SyncState>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddFile("file1.txt", new MockFileData("dummy"));
        IFileSystemAdapter fs = Substitute.For<IFileSystemAdapter>();
        fs.GetFileInfo(Arg.Any<string>()).Returns(callInfo => mockFileSystem.FileInfo.New((string)callInfo[0]));
        fs.OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Stream?>(new MemoryStream(new byte[10])));
        fs.WriteFileAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        IGraphClient graph = Substitute.For<IGraphClient>();
        graph.CreateUploadSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new UploadSessionInfo("url", "id", DateTimeOffset.UtcNow.AddMinutes(5)));
        graph.UploadChunkAsync(Arg.Any<UploadSessionInfo>(), Arg.Any<Stream>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new OperationCanceledException());

        ILogger<TransferService> logger = Substitute.For<ILogger<TransferService>>();
        UserPreferences settings = new()
        {
            UiSettings = new UiSettings
            {
                SyncSettings = new SyncSettings
                {
                    MaxParallelDownloads = 2,
                    DownloadBatchSize = 1,
                    MaxRetries = 1,
                    RetryBaseDelayMs = 1
                }
            }
        };
        TransferService sut = CreateSut(fs: fs, graph: graph, repo: repo, logger: logger, settings: settings);

        using var cts = new CancellationTokenSource();

        try
        {
            await sut.ProcessPendingUploadsAsync(cts.Token);
        }
        catch(OperationCanceledException)
        {
            // Expected, ignore
        }

        logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task NotThrowWhenNoPendingUploads()
    {
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        repo.GetPendingUploadsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        TransferService sut = CreateSut(repo: repo);

        await Should.NotThrowAsync(() => sut.ProcessPendingUploadsAsync(TestContext.Current.CancellationToken));
    }
}
