using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit;

public class SyncEngineShould
{
    private static (
        SyncEngine sut,
        ISyncRepository repo,
        IGraphClient graph,
        ITransferService transfer,
        IFileSystemAdapter fs,
        ILogger<SyncEngine> logger
    ) CreateSut()
    {
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        IGraphClient graph = Substitute.For<IGraphClient>();
        ITransferService transfer = Substitute.For<ITransferService>();
        IFileSystemAdapter fs = Substitute.For<IFileSystemAdapter>();
        ILogger<SyncEngine> logger = Substitute.For<ILogger<SyncEngine>>();
        var sut = new SyncEngine(repo, graph, transfer, fs, logger);
        return (sut, repo, graph, transfer, fs, logger);
    }

    [Fact]
    public async Task MarkNewLocalFileAsPendingUpload()
    {
        (SyncEngine? sut, ISyncRepository? repo, IGraphClient _, ITransferService _, IFileSystemAdapter? fs, ILogger<SyncEngine> _) = CreateSut();
        var localFile = new LocalFileInfo("new.txt", 100, DateTimeOffset.UtcNow, "hash1");
        fs.EnumerateFilesAsync(Arg.Any<CancellationToken>()).Returns([localFile]);
        repo.GetLocalFileByPathAsync("new.txt", Arg.Any<CancellationToken>()).Returns((LocalFileRecord?)null);
        repo.GetDriveItemByPathAsync("new.txt", Arg.Any<CancellationToken>()).Returns((DriveItemRecord?)null);
        repo.GetPendingUploadCountAsync(Arg.Any<CancellationToken>()).Returns(1);

        await sut.ScanLocalFilesAsync(CancellationToken.None);

        await repo.Received().AddOrUpdateLocalFileAsync(
            Arg.Is<LocalFileRecord>(f => f.RelativePath == "new.txt" && f.SyncState == SyncState.PendingUpload),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitialFullSyncAsync_EmitsProgressAndCompletesTransfers()
    {
        (SyncEngine? sut, ISyncRepository? repo, IGraphClient? graph, ITransferService? transfer, IFileSystemAdapter _, ILogger<SyncEngine> _) = CreateSut();
        var page1 = new DeltaPage([
            new DriveItemRecord("id1", "did1", "file1.txt", null, null, 100, DateTimeOffset.UtcNow, false, false)
        ], null, "delta1");
        graph.GetDriveDeltaPageAsync(null, Arg.Any<CancellationToken>()).Returns(page1);
        repo.ApplyDriveItemsAsync(page1.Items, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        repo.SaveOrUpdateDeltaTokenAsync(Arg.Any<DeltaToken>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(1);
        repo.GetPendingUploadCountAsync(Arg.Any<CancellationToken>()).Returns(2);
        transfer.ProcessPendingDownloadsAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        transfer.ProcessPendingUploadsAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var progressEvents = new List<SyncProgress>();
        using IDisposable sub = sut.Progress.Subscribe(progressEvents.Add);
        await sut.InitialFullSyncAsync(CancellationToken.None);

        progressEvents.Count.ShouldBeGreaterThan(0);
        await repo.Received().ApplyDriveItemsAsync(page1.Items, Arg.Any<CancellationToken>());
        await repo.Received().SaveOrUpdateDeltaTokenAsync(Arg.Any<DeltaToken>(), Arg.Any<CancellationToken>());
        await transfer.Received().ProcessPendingDownloadsAsync(Arg.Any<CancellationToken>());
        await transfer.Received().ProcessPendingUploadsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitialFullSyncAsync_HandlesEmptyDeltaPages()
    {
        (SyncEngine? sut, ISyncRepository? repo, IGraphClient? graph, ITransferService? transfer, IFileSystemAdapter _, ILogger<SyncEngine> _) = CreateSut();
        var page1 = new DeltaPage([], null, "delta1");
        graph.GetDriveDeltaPageAsync(null, Arg.Any<CancellationToken>()).Returns(page1);
        repo.ApplyDriveItemsAsync(page1.Items, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        repo.SaveOrUpdateDeltaTokenAsync(Arg.Any<DeltaToken>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(0);
        repo.GetPendingUploadCountAsync(Arg.Any<CancellationToken>()).Returns(0);
        transfer.ProcessPendingDownloadsAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        transfer.ProcessPendingUploadsAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var progressEvents = new List<SyncProgress>();
        using IDisposable sub = sut.Progress.Subscribe(progressEvents.Add);
        await sut.InitialFullSyncAsync(CancellationToken.None);

        progressEvents.Count.ShouldBeGreaterThan(0);
        await repo.Received().ApplyDriveItemsAsync(page1.Items, Arg.Any<CancellationToken>());
        await repo.Received().SaveOrUpdateDeltaTokenAsync(Arg.Any<DeltaToken>(), Arg.Any<CancellationToken>());
        await transfer.Received().ProcessPendingDownloadsAsync(Arg.Any<CancellationToken>());
        await transfer.Received().ProcessPendingUploadsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitialFullSyncAsync_HandlesCancellation()
    {
        (SyncEngine? sut, ISyncRepository? repo, IGraphClient? graph, ITransferService _, IFileSystemAdapter _, ILogger<SyncEngine> _) = CreateSut();
        var page1 = new DeltaPage([
            new DriveItemRecord("id1", "did1", "file1.txt", null, null, 100, DateTimeOffset.UtcNow, false, false)
        ], null, "delta1");
        graph.GetDriveDeltaPageAsync(null, Arg.Any<CancellationToken>()).Returns(async ci =>
        {
            CancellationToken ct = ci.ArgAt<CancellationToken>(1);
            ct.ThrowIfCancellationRequested();
            return page1;
        });
        repo.ApplyDriveItemsAsync(page1.Items, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => sut.InitialFullSyncAsync(cts.Token));
    }

    [Fact]
    public async Task IncrementalSyncAsync_EmitsProgressAndCompletesTransfers()
    {
        (SyncEngine? sut, ISyncRepository? repo, IGraphClient? graph, ITransferService? transfer, IFileSystemAdapter _, ILogger<SyncEngine> _) = CreateSut();
        var token = new DeltaToken("tok", "delta", DateTimeOffset.UtcNow);
        var page = new DeltaPage([
            new DriveItemRecord("id1", "did1", "file1.txt", null, null, 100, DateTimeOffset.UtcNow, false, false)
        ], null, "delta2");
        repo.GetDeltaTokenAsync(Arg.Any<CancellationToken>()).Returns(token);
        graph.GetDriveDeltaPageAsync(token.Token, Arg.Any<CancellationToken>()).Returns(page);
        repo.ApplyDriveItemsAsync(page.Items, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        repo.SaveOrUpdateDeltaTokenAsync(Arg.Any<DeltaToken>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(1);
        repo.GetPendingUploadCountAsync(Arg.Any<CancellationToken>()).Returns(2);
        transfer.ProcessPendingDownloadsAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        transfer.ProcessPendingUploadsAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var progressEvents = new List<SyncProgress>();
        using IDisposable sub = sut.Progress.Subscribe(progressEvents.Add);
        await sut.IncrementalSyncAsync(CancellationToken.None);

        progressEvents.Count.ShouldBeGreaterThan(0);
        await repo.Received().ApplyDriveItemsAsync(page.Items, Arg.Any<CancellationToken>());
        await repo.Received().SaveOrUpdateDeltaTokenAsync(Arg.Any<DeltaToken>(), Arg.Any<CancellationToken>());
        await transfer.Received().ProcessPendingDownloadsAsync(Arg.Any<CancellationToken>());
        await transfer.Received().ProcessPendingUploadsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IncrementalSyncAsync_ThrowsIfDeltaTokenMissing()
    {
        (SyncEngine? sut, ISyncRepository? repo, IGraphClient _, ITransferService _, IFileSystemAdapter _, ILogger<SyncEngine> _) = CreateSut();
        repo.GetDeltaTokenAsync(Arg.Any<CancellationToken>()).Returns((DeltaToken?)null);
        await Should.ThrowAsync<InvalidOperationException>(() => sut.IncrementalSyncAsync(CancellationToken.None));
    }

    [Fact]
    public async Task IncrementalSyncAsync_HandlesErrorFromGraph()
    {
        (SyncEngine? sut, ISyncRepository? repo, IGraphClient? graph, ITransferService _, IFileSystemAdapter _, ILogger<SyncEngine> _) = CreateSut();
        var token = new DeltaToken("tok", "delta", DateTimeOffset.UtcNow);
        repo.GetDeltaTokenAsync(Arg.Any<CancellationToken>()).Returns(token);
        graph.GetDriveDeltaPageAsync(token.Token, Arg.Any<CancellationToken>()).Returns(Task.FromException<DeltaPage>(new IOException("fail")));
        await Should.ThrowAsync<IOException>(() => sut.IncrementalSyncAsync(CancellationToken.None));
    }

    [Fact]
    public async Task NotMarkUnchangedDownloadedFileAsPendingUpload()
    {
        (SyncEngine? sut, ISyncRepository? repo, IGraphClient _, ITransferService _, IFileSystemAdapter? fs, ILogger<SyncEngine> _) = CreateSut();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var localFile = new LocalFileInfo("file.txt", 100, now, "hash1");
        var driveItem = new DriveItemRecord("id1", "did1", "file.txt", "etag", "ctag", 100, now, false, false);
        var localRecord = new LocalFileRecord("id1", "file.txt", "hash1", 100, now, SyncState.Downloaded);
        fs.EnumerateFilesAsync(Arg.Any<CancellationToken>()).Returns([localFile]);
        repo.GetLocalFileByPathAsync("file.txt", Arg.Any<CancellationToken>()).Returns(localRecord);
        repo.GetDriveItemByPathAsync("file.txt", Arg.Any<CancellationToken>()).Returns(driveItem);
        repo.GetPendingUploadCountAsync(Arg.Any<CancellationToken>()).Returns(0);

        await sut.ScanLocalFilesAsync(CancellationToken.None);

        await repo.DidNotReceive().AddOrUpdateLocalFileAsync(Arg.Any<LocalFileRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkLocalFileAsPendingUploadIfNewerThanOnedrive()
    {
        (SyncEngine? sut, ISyncRepository? repo, IGraphClient _, ITransferService _, IFileSystemAdapter? fs, ILogger<SyncEngine> _) = CreateSut();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var localFile = new LocalFileInfo("file.txt", 100, now.AddMinutes(5), "hash1");
        var driveItem = new DriveItemRecord("id1", "did1", "file.txt", "etag", "ctag", 100, now, false, false);
        var localRecord = new LocalFileRecord("id1", "file.txt", "hash1", 100, now.AddMinutes(5), SyncState.Downloaded);
        fs.EnumerateFilesAsync(Arg.Any<CancellationToken>()).Returns([localFile]);
        repo.GetLocalFileByPathAsync("file.txt", Arg.Any<CancellationToken>()).Returns(localRecord);
        repo.GetDriveItemByPathAsync("file.txt", Arg.Any<CancellationToken>()).Returns(driveItem);
        repo.GetPendingUploadCountAsync(Arg.Any<CancellationToken>()).Returns(1);

        await sut.ScanLocalFilesAsync(CancellationToken.None);

        await repo.Received().AddOrUpdateLocalFileAsync(
            Arg.Is<LocalFileRecord>(f => f.RelativePath == "file.txt" && f.SyncState == SyncState.PendingUpload),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkLocalFileAsPendingUploadIfSizeDiffersFromOnedrive()
    {
        (SyncEngine? sut, ISyncRepository? repo, IGraphClient _, ITransferService _, IFileSystemAdapter? fs, ILogger<SyncEngine> _) = CreateSut();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var localFile = new LocalFileInfo("file.txt", 200, now, "hash1");
        var driveItem = new DriveItemRecord("id1", "did1", "file.txt", "etag", "ctag", 100, now, false, false);
        var localRecord = new LocalFileRecord("id1", "file.txt", "hash1", 200, now, SyncState.Downloaded);
        fs.EnumerateFilesAsync(Arg.Any<CancellationToken>()).Returns([localFile]);
        repo.GetLocalFileByPathAsync("file.txt", Arg.Any<CancellationToken>()).Returns(localRecord);
        repo.GetDriveItemByPathAsync("file.txt", Arg.Any<CancellationToken>()).Returns(driveItem);
        repo.GetPendingUploadCountAsync(Arg.Any<CancellationToken>()).Returns(1);

        await sut.ScanLocalFilesAsync(CancellationToken.None);

        await repo.Received().AddOrUpdateLocalFileAsync(
            Arg.Is<LocalFileRecord>(f => f.RelativePath == "file.txt" && f.SyncState == SyncState.PendingUpload),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Mark_local_file_as_pending_upload_if_hash_differs_from_onedrive()
    {
        (SyncEngine? sut, ISyncRepository? repo, IGraphClient _, ITransferService _, IFileSystemAdapter? fs, ILogger<SyncEngine> _) = CreateSut();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var localFile = new LocalFileInfo("file.txt", 100, now, "hash2");
        var driveItem = new DriveItemRecord("id1", "did1", "file.txt", "etag", "ctag", 100, now, false, false);
        var localRecord = new LocalFileRecord("id1", "file.txt", "hash1", 100, now, SyncState.Downloaded);
        fs.EnumerateFilesAsync(Arg.Any<CancellationToken>()).Returns([localFile]);
        repo.GetLocalFileByPathAsync("file.txt", Arg.Any<CancellationToken>()).Returns(localRecord);
        repo.GetDriveItemByPathAsync("file.txt", Arg.Any<CancellationToken>()).Returns(driveItem);
        repo.GetPendingUploadCountAsync(Arg.Any<CancellationToken>()).Returns(1);

        await sut.ScanLocalFilesAsync(CancellationToken.None);

        await repo.Received().AddOrUpdateLocalFileAsync(
            Arg.Is<LocalFileRecord>(f => f.RelativePath == "file.txt" && f.SyncState == SyncState.PendingUpload),
            Arg.Any<CancellationToken>());
    }
}
