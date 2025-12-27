using System.IO.Abstractions.TestingHelpers;
using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit;

public class TransferServiceShould
{
    private static (
        TransferService sut,
        ISyncRepository repo,
        IFileSystemAdapter fs,
        IGraphClient graph,
        ILogger<TransferService> logger,
        MockFileSystem mockFileSystem,
        UserPreferences settings
    ) CreateSut(UserPreferences? settings = null)
    {
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        var mockFileSystem = new MockFileSystem();
        IFileSystemAdapter fs = Substitute.For<IFileSystemAdapter>();
        IGraphClient graph = Substitute.For<IGraphClient>();
        ILogger<TransferService> logger = Substitute.For<ILogger<TransferService>>();
        settings ??= new UserPreferences
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

        fs.GetFileInfo(Arg.Any<string>()).Returns(callInfo => mockFileSystem.FileInfo.New((string)callInfo[0]));
        fs.WriteFileAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        fs.OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Stream?>(new MemoryStream(new byte[10])));

        repo.MarkLocalFileStateAsync(Arg.Any<string>(), Arg.Any<SyncState>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        repo.LogTransferAsync(Arg.Any<TransferLog>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        graph.DownloadDriveItemContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Stream>(new MemoryStream(new byte[10])));
        graph.CreateUploadSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new UploadSessionInfo("url", "id", DateTimeOffset.UtcNow.AddMinutes(5)));
        graph.UploadChunkAsync(Arg.Any<UploadSessionInfo>(), Arg.Any<Stream>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new TransferService(fs, graph, repo, logger, settings);
        return (sut, repo, fs, graph, logger, mockFileSystem, settings);
    }

    [Fact]
    public async Task NotReportProgressWhenNoPendingDownloads()
    {
        (TransferService? sut, ISyncRepository? repo, IFileSystemAdapter _, IGraphClient _, ILogger<TransferService> _, MockFileSystem _, UserPreferences _) = CreateSut();
        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(0);
        var progressReported = false;
        using IDisposable subscription = sut.Progress.Subscribe(_ => progressReported = true);
        await sut.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);
        progressReported.ShouldBeFalse();
    }

    [Fact]
    public async Task LogWarningWhenDownloadIsCancelled()
    {
        (TransferService? sut, ISyncRepository? repo, IFileSystemAdapter _, IGraphClient _, ILogger<TransferService>? logger, MockFileSystem _, UserPreferences _) = CreateSut();
        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(1);
        repo.GetPendingDownloadsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
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
        (TransferService? _, ISyncRepository? repo, IFileSystemAdapter fs, IGraphClient _, ILogger<TransferService> logger, MockFileSystem _, UserPreferences? settings) = CreateSut();
        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(1);
        repo.GetPendingDownloadsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([
            new DriveItemRecord("id", "did", "file.txt", null, null, 100, DateTimeOffset.UtcNow, false, false)
        ]);
        IGraphClient graph = Substitute.For<IGraphClient>();
        graph.DownloadDriveItemContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<System.IO.Stream>(new IOException("fail")));
        var sutWithFailingGraph = new TransferService(fs, graph, repo, logger, settings);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        Exception? ex = await Record.ExceptionAsync(() => sutWithFailingGraph.ProcessPendingDownloadsAsync(cts.Token));
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
        (TransferService? sut, ISyncRepository? repo, IFileSystemAdapter _, IGraphClient _, ILogger<TransferService> _, MockFileSystem? mockFileSystem, UserPreferences _) = CreateSut();
        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(2);
        var callCount = 0;
        repo.GetPendingDownloadsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ => callCount++ == 0
                ? [
                    new("id1", "did1", "file1.txt", null, null, 100, DateTimeOffset.UtcNow, false, false),
                    new("id2", "did2", "file2.txt", null, null, 200, DateTimeOffset.UtcNow, false, false)
                ]
                : []);
        mockFileSystem.AddFile("file1.txt", new MockFileData("dummy"));
        mockFileSystem.AddFile("file2.txt", new MockFileData("dummy"));
        var progressCount = 0;
        using IDisposable subscription = sut.Progress.Subscribe(_ => progressCount++);
        await sut.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);
        progressCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ReportProgressDuringUploads()
    {
        (TransferService? sut, ISyncRepository? repo, IFileSystemAdapter _, IGraphClient _, ILogger<TransferService> _, MockFileSystem? mockFileSystem, UserPreferences _) = CreateSut();
        repo.GetPendingUploadsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([
            new LocalFileRecord("id1", "file1.txt", "hash1", 100, DateTimeOffset.UtcNow, SyncState.PendingUpload),
            new LocalFileRecord("id2", "file2.txt", "hash2", 200, DateTimeOffset.UtcNow, SyncState.PendingUpload)
        ]);
        mockFileSystem.AddFile("file1.txt", new MockFileData("dummy"));
        mockFileSystem.AddFile("file2.txt", new MockFileData("dummy"));
        var progressCount = 0;
        using IDisposable subscription = sut.Progress.Subscribe(_ => progressCount++);
        await sut.ProcessPendingUploadsAsync(TestContext.Current.CancellationToken);
        progressCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task MarkUploadAsFailedWhenExceptionThrown()
    {
        (TransferService? sut, ISyncRepository? repo, IFileSystemAdapter? fs, IGraphClient _, ILogger<TransferService> _, MockFileSystem? mockFileSystem, UserPreferences _) = CreateSut();
        repo.GetPendingUploadsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([
            new LocalFileRecord("id1", "file1.txt", "hash1", 100, DateTimeOffset.UtcNow, SyncState.PendingUpload)
        ]);
        mockFileSystem.AddFile("file1.txt", new MockFileData("dummy"));
        fs.OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<Stream?>(new IOException("fail")));
        await sut.ProcessPendingUploadsAsync(TestContext.Current.CancellationToken);
        await repo.Received().LogTransferAsync(Arg.Is<TransferLog>(t => t.Status == TransferStatus.Failed), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelUploadsShouldLogWarning()
    {
        (TransferService? sut, ISyncRepository? repo, IFileSystemAdapter? fs, IGraphClient _, ILogger<TransferService>? logger, MockFileSystem? mockFileSystem, UserPreferences _) = CreateSut();
        repo.GetPendingUploadsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([
            new LocalFileRecord("id1", "file1.txt", "hash1", 100, DateTimeOffset.UtcNow, SyncState.PendingUpload)
        ]);
        mockFileSystem.AddFile("file1.txt", new MockFileData("dummy"));
        IGraphClient graph = Substitute.For<IGraphClient>();
        graph.CreateUploadSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new UploadSessionInfo("url", "id", DateTimeOffset.UtcNow.AddMinutes(5)));
        graph.UploadChunkAsync(Arg.Any<UploadSessionInfo>(), Arg.Any<Stream>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new OperationCanceledException());
        var sutWithCancel = new TransferService(fs, graph, repo, logger, new UserPreferences
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
        });
        using var cts = new CancellationTokenSource();
        try
        {
            await sutWithCancel.ProcessPendingUploadsAsync(cts.Token);
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
        (TransferService? sut, ISyncRepository? repo, IFileSystemAdapter _, IGraphClient _, ILogger<TransferService> _, MockFileSystem _, UserPreferences _) = CreateSut();
        repo.GetPendingUploadsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        await Should.NotThrowAsync(() => sut.ProcessPendingUploadsAsync(TestContext.Current.CancellationToken));
    }
}
