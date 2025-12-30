using System.IO.Abstractions.TestingHelpers;
using System.Threading.Channels;
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
        UserPreferences settings,
        SyncProgressReporter progressReporter,
        ISyncErrorLogger errorLogger,
        IChannelFactory channelFactory,
        IDownloadQueueProducer producer,
        IDownloadQueueConsumer consumer
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
        var progressReporter = new SyncProgressReporter();
        ISyncErrorLogger errorLogger = Substitute.For<ISyncErrorLogger>();
        IChannelFactory channelFactory = Substitute.For<IChannelFactory>();
        IDownloadQueueProducer producer = Substitute.For<IDownloadQueueProducer>();
        IDownloadQueueConsumer consumer = Substitute.For<IDownloadQueueConsumer>();
        var sut = new TransferService(fs, graph, repo, logger, settings, progressReporter, errorLogger, channelFactory, producer, consumer);
        return (sut, repo, fs, graph, logger, mockFileSystem, settings, progressReporter, errorLogger, channelFactory, producer, consumer);
    }

    [Fact]
    public async Task NotReportProgressWhenNoPendingDownloads()
    {
        (TransferService sut, ISyncRepository repo, IFileSystemAdapter fs, IGraphClient graph, ILogger<TransferService> logger, MockFileSystem mockFileSystem, UserPreferences settings, SyncProgressReporter progressReporter, ISyncErrorLogger errorLogger, IChannelFactory channelFactory, IDownloadQueueProducer producer, IDownloadQueueConsumer consumer) tuple = CreateSut();
        TransferService sut = tuple.sut;
        ISyncRepository repo = tuple.repo;
        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(0);
        var progressReported = false;
        using IDisposable subscription = sut.Progress.Subscribe(_ => progressReported = true);
        await sut.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);
        progressReported.ShouldBeFalse();
    }
    [Fact]
    public async Task ReportProgressWithEtaAndTotalBytes()
    {
        (TransferService? sut, ISyncRepository? repo, IFileSystemAdapter? fs, IGraphClient? graph, ILogger<TransferService>? logger, MockFileSystem? mockFileSystem, UserPreferences? settings, SyncProgressReporter? progressReporter, ISyncErrorLogger? errorLogger, IChannelFactory? channelFactory, IDownloadQueueProducer? producer, IDownloadQueueConsumer? consumer) = CreateSut();
        DriveItemRecord[] driveItems = new[]
    {
        new DriveItemRecord("id1", "did1", "file1.txt", null, null, 100, DateTimeOffset.UtcNow, false, false),
        new DriveItemRecord("id2", "did2", "file2.txt", null, null, 200, DateTimeOffset.UtcNow, false, false)
    };
        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(2);
        repo.GetAllPendingDownloadsAsync(Arg.Any<CancellationToken>()).Returns(driveItems);
        repo.GetPendingDownloadsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(driveItems);
        producer.ProduceAsync(Arg.Any<ChannelWriter<DriveItemRecord>>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        consumer.ConsumeAsync(Arg.Any<ChannelReader<DriveItemRecord>>(), Arg.Any<Func<DriveItemRecord, Task>>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(async callInfo =>
        {
            Func<DriveItemRecord, Task> process = callInfo.Arg<Func<DriveItemRecord, Task>>();
            foreach(DriveItemRecord? item in driveItems)
                await process(item);
        });
        var progressList = new List<SyncProgress>();
        using IDisposable subscription = progressReporter.Progress.Subscribe(progressList.Add);

        await sut.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

        progressList.ShouldContain(p => p.EstimatedTimeRemaining != null);
        progressList.ShouldAllBe(p => p.TotalBytes == 300);
    }

    [Fact]
    public async Task UseGetAllPendingDownloadsAsyncForTotalBytes()
    {
        (TransferService? sut, ISyncRepository? repo, IFileSystemAdapter? fs, IGraphClient? graph, ILogger<TransferService>? logger, MockFileSystem? mockFileSystem, UserPreferences? settings, SyncProgressReporter? progressReporter, ISyncErrorLogger? errorLogger, IChannelFactory? channelFactory, IDownloadQueueProducer? producer, IDownloadQueueConsumer? consumer) = CreateSut();
        DriveItemRecord[] driveItems = new[]
    {
        new DriveItemRecord("id1", "did1", "file1.txt", null, null, 100, DateTimeOffset.UtcNow, false, false),
        new DriveItemRecord("id2", "did2", "file2.txt", null, null, 200, DateTimeOffset.UtcNow, false, false),
        new DriveItemRecord("id3", "did3", "file3.txt", null, null, 300, DateTimeOffset.UtcNow, false, false)
    };
        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(3);
        repo.GetAllPendingDownloadsAsync(Arg.Any<CancellationToken>()).Returns(driveItems);
        repo.GetPendingDownloadsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(driveItems);
        producer.ProduceAsync(Arg.Any<ChannelWriter<DriveItemRecord>>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        consumer.ConsumeAsync(Arg.Any<ChannelReader<DriveItemRecord>>(), Arg.Any<Func<DriveItemRecord, Task>>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(async callInfo =>
        {
            Func<DriveItemRecord, Task> process = callInfo.Arg<Func<DriveItemRecord, Task>>();
            foreach(DriveItemRecord? item in driveItems)
                await process(item);
        });
        var progressList = new List<SyncProgress>();
        using IDisposable subscription = progressReporter.Progress.Subscribe(progressList.Add);

        await sut.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

        repo.Received(1).GetAllPendingDownloadsAsync(Arg.Any<CancellationToken>());
        progressList.ShouldAllBe(p => p.TotalBytes == 600);
    }

    [Fact]
    public async Task EtaIsNullWhenTotalTransferredExceedsTotalBytes()
    {
        (TransferService? sut, ISyncRepository? repo, IFileSystemAdapter? fs, IGraphClient? graph, ILogger<TransferService>? logger, MockFileSystem? mockFileSystem, UserPreferences? settings, SyncProgressReporter? progressReporter, ISyncErrorLogger? errorLogger, IChannelFactory? channelFactory, IDownloadQueueProducer? producer, IDownloadQueueConsumer? consumer) = CreateSut();
        DriveItemRecord[] driveItems = new[]
    {
        new DriveItemRecord("id1", "did1", "file1.txt", null, null, 100, DateTimeOffset.UtcNow, false, false)
    };
        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(1);
        repo.GetAllPendingDownloadsAsync(Arg.Any<CancellationToken>()).Returns(driveItems);
        repo.GetPendingDownloadsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(driveItems);
        producer.ProduceAsync(Arg.Any<ChannelWriter<DriveItemRecord>>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        consumer.ConsumeAsync(Arg.Any<ChannelReader<DriveItemRecord>>(), Arg.Any<Func<DriveItemRecord, Task>>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(async callInfo =>
        {
            Func<DriveItemRecord, Task> process = callInfo.Arg<Func<DriveItemRecord, Task>>();
            // Simulate totalTransferred > totalBytes
            await process(new DriveItemRecord("id1", "did1", "file1.txt", null, null, 200, DateTimeOffset.UtcNow, false, false));
        });
        var progressList = new List<SyncProgress>();
        using IDisposable subscription = progressReporter.Progress.Subscribe(progressList.Add);

        await sut.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);

        progressList.ShouldAllBe(p => p.EstimatedTimeRemaining == null || p.EstimatedTimeRemaining >= TimeSpan.Zero);
    }
    [Fact]
    public async Task LogWarningWhenDownloadIsCancelled()
    {
        (TransferService sut, ISyncRepository repo, IFileSystemAdapter fs, IGraphClient graph, ILogger<TransferService> logger, MockFileSystem mockFileSystem, UserPreferences settings, SyncProgressReporter progressReporter, ISyncErrorLogger errorLogger, IChannelFactory channelFactory, IDownloadQueueProducer producer, IDownloadQueueConsumer consumer) tuple = CreateSut();
        TransferService sut = tuple.sut;
        ISyncRepository repo = tuple.repo;
        ILogger<TransferService> logger = tuple.logger;
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
        (TransferService sut, ISyncRepository repo, IFileSystemAdapter fs, IGraphClient graph, ILogger<TransferService> logger, MockFileSystem mockFileSystem, UserPreferences settings, SyncProgressReporter progressReporter, ISyncErrorLogger errorLogger, IChannelFactory channelFactory, IDownloadQueueProducer producer, IDownloadQueueConsumer consumer) tuple = CreateSut();
        ISyncRepository repo = tuple.repo;
        IFileSystemAdapter fs = tuple.fs;
        ILogger<TransferService> logger = tuple.logger;
        UserPreferences settings = tuple.settings;
        SyncProgressReporter progressReporter = tuple.progressReporter;
        ISyncErrorLogger errorLogger = tuple.errorLogger;
        IChannelFactory channelFactory = tuple.channelFactory;
        IDownloadQueueProducer producer = tuple.producer;
        IDownloadQueueConsumer consumer = tuple.consumer;
        IGraphClient graph = Substitute.For<IGraphClient>();
        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(1);
        repo.GetPendingDownloadsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([
            new DriveItemRecord("id", "did", "file.txt", null, null, 100, DateTimeOffset.UtcNow, false, false)
        ]);
        graph.DownloadDriveItemContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Stream>(new IOException("fail")));
        var sutWithFailingGraph = new TransferService(fs, graph, repo, logger, settings, progressReporter, errorLogger, channelFactory, producer, consumer);
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
        (TransferService sut, ISyncRepository repo, IFileSystemAdapter fs, IGraphClient graph, ILogger<TransferService> logger, MockFileSystem mockFileSystem, UserPreferences settings, SyncProgressReporter progressReporter, ISyncErrorLogger errorLogger, IChannelFactory channelFactory, IDownloadQueueProducer producer, IDownloadQueueConsumer consumer) tuple = CreateSut();
        TransferService sut = tuple.sut;
        ISyncRepository repo = tuple.repo;
        MockFileSystem mockFileSystem = tuple.mockFileSystem;
        SyncProgressReporter progressReporter = tuple.progressReporter;
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
        using IDisposable subscription = progressReporter.Progress.Subscribe(_ => progressCount++);
        await sut.ProcessPendingDownloadsAsync(TestContext.Current.CancellationToken);
        progressCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ReportProgressDuringUploads()
    {
        (TransferService sut, ISyncRepository repo, IFileSystemAdapter fs, IGraphClient graph, ILogger<TransferService> logger, MockFileSystem mockFileSystem, UserPreferences settings, SyncProgressReporter progressReporter, ISyncErrorLogger errorLogger, IChannelFactory channelFactory, IDownloadQueueProducer producer, IDownloadQueueConsumer consumer) tuple = CreateSut();
        TransferService sut = tuple.sut;
        ISyncRepository repo = tuple.repo;
        MockFileSystem mockFileSystem = tuple.mockFileSystem;
        SyncProgressReporter progressReporter = tuple.progressReporter;
        repo.GetPendingUploadsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([
            new LocalFileRecord("id1", "file1.txt", "hash1", 100, DateTimeOffset.UtcNow, SyncState.PendingUpload),
            new LocalFileRecord("id2", "file2.txt", "hash2", 200, DateTimeOffset.UtcNow, SyncState.PendingUpload)
        ]);
        mockFileSystem.AddFile("file1.txt", new MockFileData("dummy"));
        mockFileSystem.AddFile("file2.txt", new MockFileData("dummy"));
        var progressCount = 0;
        using IDisposable subscription = progressReporter.Progress.Subscribe(_ => progressCount++);
        await sut.ProcessPendingUploadsAsync(TestContext.Current.CancellationToken);
        progressCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task MarkUploadAsFailedWhenExceptionThrown()
    {
        (TransferService sut, ISyncRepository repo, IFileSystemAdapter fs, IGraphClient graph, ILogger<TransferService> logger, MockFileSystem mockFileSystem, UserPreferences settings, SyncProgressReporter progressReporter, ISyncErrorLogger errorLogger, IChannelFactory channelFactory, IDownloadQueueProducer producer, IDownloadQueueConsumer consumer) tuple = CreateSut();
        TransferService sut = tuple.sut;
        ISyncRepository repo = tuple.repo;
        IFileSystemAdapter fs = tuple.fs;
        MockFileSystem mockFileSystem = tuple.mockFileSystem;
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
        (TransferService sut, ISyncRepository repo, IFileSystemAdapter fs, IGraphClient graph, ILogger<TransferService> logger, MockFileSystem mockFileSystem, UserPreferences settings, SyncProgressReporter progressReporter, ISyncErrorLogger errorLogger, IChannelFactory channelFactory, IDownloadQueueProducer producer, IDownloadQueueConsumer consumer) tuple = CreateSut();
        ISyncRepository repo = tuple.repo;
        IFileSystemAdapter fs = tuple.fs;
        ILogger<TransferService> logger = tuple.logger;
        MockFileSystem mockFileSystem = tuple.mockFileSystem;
        UserPreferences settings = tuple.settings;
        SyncProgressReporter progressReporter = tuple.progressReporter;
        ISyncErrorLogger errorLogger = tuple.errorLogger;
        IChannelFactory channelFactory = tuple.channelFactory;
        IDownloadQueueProducer producer = tuple.producer;
        IDownloadQueueConsumer consumer = tuple.consumer;
        IGraphClient graph = Substitute.For<IGraphClient>();
        repo.GetPendingUploadsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([
            new LocalFileRecord("id1", "file1.txt", "hash1", 100, DateTimeOffset.UtcNow, SyncState.PendingUpload)
        ]);
        mockFileSystem.AddFile("file1.txt", new MockFileData("dummy"));
        graph.CreateUploadSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new UploadSessionInfo("url", "id", DateTimeOffset.UtcNow.AddMinutes(5)));
        graph.UploadChunkAsync(Arg.Any<UploadSessionInfo>(), Arg.Any<Stream>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new OperationCanceledException());
        var sutWithCancel = new TransferService(fs, graph, repo, logger, settings, progressReporter, errorLogger, channelFactory, producer, consumer);
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
        (TransferService sut, ISyncRepository repo, IFileSystemAdapter fs, IGraphClient graph, ILogger<TransferService> logger, MockFileSystem mockFileSystem, UserPreferences settings, SyncProgressReporter progressReporter, ISyncErrorLogger errorLogger, IChannelFactory channelFactory, IDownloadQueueProducer producer, IDownloadQueueConsumer consumer) tuple = CreateSut();
        TransferService sut = tuple.sut;
        ISyncRepository repo = tuple.repo;
        repo.GetPendingUploadsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        await Should.NotThrowAsync(() => sut.ProcessPendingUploadsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReportProgressWithEtaAndTotalBytesForUploads()
    {
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        IFileSystemAdapter fs = Substitute.For<IFileSystemAdapter>();
        IGraphClient graph = Substitute.For<IGraphClient>();
        ILogger<TransferService> logger = Substitute.For<ILogger<TransferService>>();
        UserPreferences settings = new UserPreferences
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
        SyncProgressReporter progressReporter = new SyncProgressReporter();
        ISyncErrorLogger errorLogger = Substitute.For<ISyncErrorLogger>();
        IChannelFactory channelFactory = Substitute.For<IChannelFactory>();
        IDownloadQueueProducer downloadProducer = Substitute.For<IDownloadQueueProducer>();
        IDownloadQueueConsumer downloadConsumer = Substitute.For<IDownloadQueueConsumer>();
        IUploadQueueProducer uploadProducer = Substitute.For<IUploadQueueProducer>();
        IUploadQueueConsumer uploadConsumer = Substitute.For<IUploadQueueConsumer>();
        var uploads = new[]
        {
            new LocalFileRecord("id1", "file1.txt", "hash1", 100, DateTimeOffset.UtcNow, SyncState.PendingUpload),
            new LocalFileRecord("id2", "file2.txt", "hash2", 200, DateTimeOffset.UtcNow, SyncState.PendingUpload)
        };
        repo.GetPendingUploadsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(uploads);
        uploadProducer.ProduceAsync(Arg.Any<ChannelWriter<LocalFileRecord>>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        uploadConsumer.ConsumeAsync(Arg.Any<ChannelReader<LocalFileRecord>>(), Arg.Any<Func<LocalFileRecord, Task>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var process = callInfo.Arg<Func<LocalFileRecord, Task>>();
                foreach (var item in uploads) await process(item);
            });
        var sut = new TransferService(fs, graph, repo, logger, settings, progressReporter, errorLogger, channelFactory, downloadProducer, downloadConsumer, uploadProducer, uploadConsumer);
        var progressList = new List<SyncProgress>();
        using IDisposable subscription = progressReporter.Progress.Subscribe(progressList.Add);
        await sut.ProcessPendingUploadsAsync(CancellationToken.None);
        progressList.ShouldContain(p => p.EstimatedTimeRemaining != null);
        progressList.ShouldAllBe(p => p.TotalBytes == 300);
    }

    [Fact]
    public async Task ReportChunkedProgressDuringUpload()
    {
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        IFileSystemAdapter fs = Substitute.For<IFileSystemAdapter>();
        IGraphClient graph = Substitute.For<IGraphClient>();
        ILogger<TransferService> logger = Substitute.For<ILogger<TransferService>>();
        UserPreferences settings = new UserPreferences
        {
            UiSettings = new UiSettings
            {
                SyncSettings = new SyncSettings
                {
                    MaxParallelDownloads = 1,
                    DownloadBatchSize = 1,
                    MaxRetries = 1,
                    RetryBaseDelayMs = 1
                }
            }
        };
        SyncProgressReporter progressReporter = new SyncProgressReporter();
        ISyncErrorLogger errorLogger = Substitute.For<ISyncErrorLogger>();
        IChannelFactory channelFactory = Substitute.For<IChannelFactory>();
        IDownloadQueueProducer downloadProducer = Substitute.For<IDownloadQueueProducer>();
        IDownloadQueueConsumer downloadConsumer = Substitute.For<IDownloadQueueConsumer>();
        IUploadQueueProducer uploadProducer = Substitute.For<IUploadQueueProducer>();
        IUploadQueueConsumer uploadConsumer = Substitute.For<IUploadQueueConsumer>();
        var upload = new LocalFileRecord("id1", "file1.txt", "hash1", 20 * 1024 * 1024, DateTimeOffset.UtcNow, SyncState.PendingUpload); // 20MB
        repo.GetPendingUploadsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new[] { upload });
        uploadProducer.ProduceAsync(Arg.Any<ChannelWriter<LocalFileRecord>>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        uploadConsumer.ConsumeAsync(Arg.Any<ChannelReader<LocalFileRecord>>(), Arg.Any<Func<LocalFileRecord, Task>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var process = callInfo.Arg<Func<LocalFileRecord, Task>>();
                await process(upload);
            });
        var sut = new TransferService(fs, graph, repo, logger, settings, progressReporter, errorLogger, channelFactory, downloadProducer, downloadConsumer, uploadProducer, uploadConsumer);
        var progressList = new List<SyncProgress>();
        using IDisposable subscription = progressReporter.Progress.Subscribe(progressList.Add);
        await sut.ProcessPendingUploadsAsync(CancellationToken.None);
        progressList.Count.ShouldBeGreaterThan(0);
        progressList.ShouldContain(p => p.CurrentOperationMessage.Contains("Uploading"));
    }
}
