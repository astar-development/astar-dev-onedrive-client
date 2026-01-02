using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit;

public class SyncEngineShould
{
    private static (
        SyncEngine sut,
        IDeltaPageProcessor deltaPageProcessor,
        ILocalFileScanner localFileScanner,
        ITransferService transfer,
        ILogger<SyncEngine> logger
    ) CreateSut()
    {
        IDeltaPageProcessor deltaPageProcessor = Substitute.For<IDeltaPageProcessor>();
        ILocalFileScanner localFileScanner = Substitute.For<ILocalFileScanner>();
        ITransferService transfer = Substitute.For<ITransferService>();
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        ILogger<SyncEngine> logger = Substitute.For<ILogger<SyncEngine>>();
        var sut = new SyncEngine(deltaPageProcessor, localFileScanner, transfer, repo, logger);
        return (sut, deltaPageProcessor, localFileScanner, transfer, logger);
    }

    [Fact]
    public async Task InitialFullSyncAsync_DelegatesToDependencies()
    {
        (SyncEngine? sut, IDeltaPageProcessor? deltaPageProcessor, ILocalFileScanner _, ITransferService? transfer, ILogger<SyncEngine> _) = CreateSut();
        CancellationToken token = CancellationToken.None;
        var deltaToken = new DeltaToken("deltaId", "anotherId", DateTimeOffset.MinValue);
        deltaPageProcessor.ProcessAllDeltaPagesAsync(deltaToken, token, Arg.Any<Action<SyncProgress>>())
            .Returns(Task.FromResult<(DeltaToken, int, int)>((deltaToken, 1, 1)));
        transfer.ProcessPendingDownloadsAsync(token).Returns(Task.CompletedTask);
        transfer.ProcessPendingUploadsAsync(token).Returns(Task.CompletedTask);
        await sut.InitialFullSyncAsync(token);

        await deltaPageProcessor.Received(1).ProcessAllDeltaPagesAsync(Arg.Any<DeltaToken>(), token, Arg.Any<Action<SyncProgress>>());
        await transfer.Received(1).ProcessPendingDownloadsAsync(token);
        await transfer.Received(1).ProcessPendingUploadsAsync(token);
    }

    [Fact]
    public async Task IncrementalSyncAsync_DelegatesToDependencies()
    {
        (SyncEngine? sut, IDeltaPageProcessor? deltaPageProcessor, ILocalFileScanner _, ITransferService? transfer, ILogger<SyncEngine> _) = CreateSut();
        CancellationToken token = CancellationToken.None;
        var deltaToken = new DeltaToken("deltaId", "anotherId", DateTimeOffset.MinValue);
        deltaPageProcessor.ProcessAllDeltaPagesAsync(deltaToken, token, Arg.Any<Action<SyncProgress>>())
            .Returns(Task.FromResult<(DeltaToken, int, int)>((deltaToken, 1, 1)));
        transfer.ProcessPendingDownloadsAsync(token).Returns(Task.CompletedTask);
        transfer.ProcessPendingUploadsAsync(token).Returns(Task.CompletedTask);
        await sut.IncrementalSyncAsync(deltaToken, token);

        await deltaPageProcessor.Received(1).ProcessAllDeltaPagesAsync(deltaToken, token, Arg.Any<Action<SyncProgress>>());
        await transfer.Received(1).ProcessPendingDownloadsAsync(token);
        await transfer.Received(1).ProcessPendingUploadsAsync(token);
    }

    [Fact]
    public async Task ScanLocalFilesAsync_DelegatesToDependency()
    {
        (SyncEngine? sut, IDeltaPageProcessor _, ILocalFileScanner? localFileScanner, ITransferService _, ILogger<SyncEngine> _) = CreateSut();
        CancellationToken token = CancellationToken.None;
        localFileScanner.ScanAndSyncLocalFilesAsync(token)
            .Returns(Task.FromResult((1, 1, 0)));

        await sut.ScanLocalFilesAsync(token);

        await localFileScanner.Received(1).ScanAndSyncLocalFilesAsync(token);
    }
    [Fact]
    public async Task EmitsProgressWithStatsOnInitialFullSync()
    {
        (SyncEngine sut, IDeltaPageProcessor deltaPageProcessor, ILocalFileScanner localFileScanner, ITransferService transfer, ILogger<SyncEngine> logger) = CreateSut();
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        // Use a new instance to inject the repo mock
        sut = new SyncEngine(deltaPageProcessor, localFileScanner, transfer, repo, logger);

        // Stub Progress observable to avoid NullReferenceException
        var progressSubject = new System.Reactive.Subjects.Subject<SyncProgress>();
        transfer.Progress.Returns(progressSubject);

        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(5);
        repo.GetPendingUploadCountAsync(Arg.Any<CancellationToken>()).Returns(2);
        var deltaToken = new DeltaToken("deltaId", "anotherId", DateTimeOffset.MinValue);
        deltaPageProcessor.ProcessAllDeltaPagesAsync(deltaToken, Arg.Any<CancellationToken>(), Arg.Any<Action<SyncProgress>>())
            .Returns(Task.FromResult<(DeltaToken finalDelta, int pageCount, int totalItemsProcessed)>((deltaToken, 1, 1)));
        transfer.ProcessPendingDownloadsAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        transfer.ProcessPendingUploadsAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var progressList = new List<SyncProgress>();
        using IDisposable subscription = sut.Progress.Subscribe(progressList.Add);

        await sut.InitialFullSyncAsync(CancellationToken.None);

        progressList.ShouldContain(p => p.PendingDownloads == 5 && p.PendingUploads == 2);
        progressList.ShouldContain(p => p.OperationType == SyncOperationType.Completed);
    }

    [Fact]
    public async Task EmitsFailedProgressOnInitialFullSyncException()
    {
        (SyncEngine sut, IDeltaPageProcessor deltaPageProcessor, ILocalFileScanner localFileScanner, ITransferService transfer, ILogger<SyncEngine> logger) = CreateSut();
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        sut = new SyncEngine(deltaPageProcessor, localFileScanner, transfer, repo, logger);

        // Stub Progress observable to avoid NullReferenceException
        var progressSubject = new System.Reactive.Subjects.Subject<SyncProgress>();
        transfer.Progress.Returns(progressSubject);

        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(1);
        repo.GetPendingUploadCountAsync(Arg.Any<CancellationToken>()).Returns(1);
        deltaPageProcessor.ProcessAllDeltaPagesAsync(Arg.Any<DeltaToken>(), Arg.Any<CancellationToken>(), Arg.Any<Action<SyncProgress>>())
            .Returns<Task<(DeltaToken finalDelta, int pageCount, int totalItemsProcessed)>>(x => throw new InvalidOperationException("fail"));
        var progressList = new List<SyncProgress>();
        using IDisposable subscription = sut.Progress.Subscribe(progressList.Add);

        IOException ex = await Should.ThrowAsync<IOException>(() => sut.InitialFullSyncAsync(CancellationToken.None));
        ex.Message.ShouldContain("Initial full sync failed");
        progressList.ShouldContain(p => p.OperationType == SyncOperationType.Failed);
    }
}
