using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit;

public class SyncEngineShould
{
    private readonly ISyncRepository _repo = Substitute.For<ISyncRepository>();
    private readonly IGraphClient _graph = Substitute.For<IGraphClient>();
    private readonly ITransferService _transfer = Substitute.For<ITransferService>();
    private readonly IFileSystemAdapter _fs = Substitute.For<IFileSystemAdapter>();
    private readonly ILogger<SyncEngine> _logger = Substitute.For<ILogger<SyncEngine>>();

    private SyncEngine CreateSut() => new(_repo, _graph, _transfer, _fs, _logger);

    [Fact]
    public async Task MarkNewLocalFileAsPendingUpload()
    {
        var localFile = new LocalFileInfo("new.txt", 100, DateTimeOffset.UtcNow, "hash1");
        _fs.EnumerateFilesAsync(Arg.Any<CancellationToken>()).Returns([localFile]);
        _repo.GetLocalFileByPathAsync("new.txt", Arg.Any<CancellationToken>()).Returns((LocalFileRecord?)null);
        _repo.GetDriveItemByPathAsync("new.txt", Arg.Any<CancellationToken>()).Returns((DriveItemRecord?)null);
        _repo.GetPendingUploadCountAsync(Arg.Any<CancellationToken>()).Returns(1);

        SyncEngine sut = CreateSut();
        await sut.ScanLocalFilesAsync(CancellationToken.None);

        await _repo.Received().AddOrUpdateLocalFileAsync(
            Arg.Is<LocalFileRecord>(f => f.RelativePath == "new.txt" && f.SyncState == SyncState.PendingUpload),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotMarkUnchangedDownloadedFileAsPendingUpload()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var localFile = new LocalFileInfo("file.txt", 100, now, "hash1");
        var driveItem = new DriveItemRecord("id1", "did1", "file.txt", "etag", "ctag", 100, now, false, false);
        var localRecord = new LocalFileRecord("id1", "file.txt", "hash1", 100, now, SyncState.Downloaded);
        _fs.EnumerateFilesAsync(Arg.Any<CancellationToken>()).Returns([localFile]);
        _repo.GetLocalFileByPathAsync("file.txt", Arg.Any<CancellationToken>()).Returns(localRecord);
        _repo.GetDriveItemByPathAsync("file.txt", Arg.Any<CancellationToken>()).Returns(driveItem);
        _repo.GetPendingUploadCountAsync(Arg.Any<CancellationToken>()).Returns(0);

        SyncEngine sut = CreateSut();
        await sut.ScanLocalFilesAsync(CancellationToken.None);

        await _repo.DidNotReceive().AddOrUpdateLocalFileAsync(Arg.Any<LocalFileRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkLocalFileAsPendingUploadIfNewerThanOnedrive()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var localFile = new LocalFileInfo("file.txt", 100, now.AddMinutes(5), "hash1");
        var driveItem = new DriveItemRecord("id1", "did1", "file.txt", "etag", "ctag", 100, now, false, false);
        var localRecord = new LocalFileRecord("id1", "file.txt", "hash1", 100, now.AddMinutes(5), SyncState.Downloaded);
        _fs.EnumerateFilesAsync(Arg.Any<CancellationToken>()).Returns([localFile]);
        _repo.GetLocalFileByPathAsync("file.txt", Arg.Any<CancellationToken>()).Returns(localRecord);
        _repo.GetDriveItemByPathAsync("file.txt", Arg.Any<CancellationToken>()).Returns(driveItem);
        _repo.GetPendingUploadCountAsync(Arg.Any<CancellationToken>()).Returns(1);

        SyncEngine sut = CreateSut();
        await sut.ScanLocalFilesAsync(CancellationToken.None);

        await _repo.Received().AddOrUpdateLocalFileAsync(
            Arg.Is<LocalFileRecord>(f => f.RelativePath == "file.txt" && f.SyncState == SyncState.PendingUpload),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkLocalFileAsPendingUploadIfSizeDiffersFromOnedrive()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var localFile = new LocalFileInfo("file.txt", 200, now, "hash1");
        var driveItem = new DriveItemRecord("id1", "did1", "file.txt", "etag", "ctag", 100, now, false, false);
        var localRecord = new LocalFileRecord("id1", "file.txt", "hash1", 200, now, SyncState.Downloaded);
        _fs.EnumerateFilesAsync(Arg.Any<CancellationToken>()).Returns([localFile]);
        _repo.GetLocalFileByPathAsync("file.txt", Arg.Any<CancellationToken>()).Returns(localRecord);
        _repo.GetDriveItemByPathAsync("file.txt", Arg.Any<CancellationToken>()).Returns(driveItem);
        _repo.GetPendingUploadCountAsync(Arg.Any<CancellationToken>()).Returns(1);

        SyncEngine sut = CreateSut();
        await sut.ScanLocalFilesAsync(CancellationToken.None);

        await _repo.Received().AddOrUpdateLocalFileAsync(
            Arg.Is<LocalFileRecord>(f => f.RelativePath == "file.txt" && f.SyncState == SyncState.PendingUpload),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Mark_local_file_as_pending_upload_if_hash_differs_from_onedrive()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var localFile = new LocalFileInfo("file.txt", 100, now, "hash2");
        var driveItem = new DriveItemRecord("id1", "did1", "file.txt", "etag", "ctag", 100, now, false, false);
        var localRecord = new LocalFileRecord("id1", "file.txt", "hash1", 100, now, SyncState.Downloaded);
        _fs.EnumerateFilesAsync(Arg.Any<CancellationToken>()).Returns([localFile]);
        _repo.GetLocalFileByPathAsync("file.txt", Arg.Any<CancellationToken>()).Returns(localRecord);
        _repo.GetDriveItemByPathAsync("file.txt", Arg.Any<CancellationToken>()).Returns(driveItem);
        _repo.GetPendingUploadCountAsync(Arg.Any<CancellationToken>()).Returns(1);

        SyncEngine sut = CreateSut();
        await sut.ScanLocalFilesAsync(CancellationToken.None);

        await _repo.Received().AddOrUpdateLocalFileAsync(
            Arg.Is<LocalFileRecord>(f => f.RelativePath == "file.txt" && f.SyncState == SyncState.PendingUpload),
            Arg.Any<CancellationToken>());
    }
}
