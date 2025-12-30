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
        deltaPageProcessor.ProcessAllDeltaPagesAsync(token)
            .Returns(Task.FromResult<(string?, int, int)>(("delta", 1, 1)));
        transfer.ProcessPendingDownloadsAsync(token).Returns(Task.CompletedTask);
        transfer.ProcessPendingUploadsAsync(token).Returns(Task.CompletedTask);

        await sut.InitialFullSyncAsync(token);

        await deltaPageProcessor.Received(1).ProcessAllDeltaPagesAsync(token);
        await transfer.Received(1).ProcessPendingDownloadsAsync(token);
        await transfer.Received(1).ProcessPendingUploadsAsync(token);
    }

    [Fact]
    public async Task IncrementalSyncAsync_DelegatesToDependencies()
    {
        (SyncEngine? sut, IDeltaPageProcessor? deltaPageProcessor, ILocalFileScanner _, ITransferService? transfer, ILogger<SyncEngine> _) = CreateSut();
        CancellationToken token = CancellationToken.None;
        deltaPageProcessor.ProcessAllDeltaPagesAsync(token)
            .Returns(Task.FromResult<(string?, int, int)>(("delta", 1, 1)));
        transfer.ProcessPendingDownloadsAsync(token).Returns(Task.CompletedTask);
        transfer.ProcessPendingUploadsAsync(token).Returns(Task.CompletedTask);

        await sut.IncrementalSyncAsync(token);

        await deltaPageProcessor.Received(1).ProcessAllDeltaPagesAsync(token);
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
}
