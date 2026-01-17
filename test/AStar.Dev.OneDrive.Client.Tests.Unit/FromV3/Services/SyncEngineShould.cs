using AStar.Dev.OneDrive.Client.FromV3;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Models.Enums;
using AStar.Dev.OneDrive.Client.FromV3.OneDriveServices;
using AStar.Dev.OneDrive.Client.FromV3.Repositories;
using AStar.Dev.OneDrive.Client.SyncConflicts;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.FromV3.Services;

public class SyncEngineShould
{
    [Fact]
    public async Task UploadFiles_BatchedDbUpdates_UsesSaveBatchAsync()
    {
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        var filesToUpload = new List<FileMetadata>();
        for(var i = 0; i < 120; i++)
        {
            filesToUpload.Add(new FileMetadata(
                $"id_{i}", "acc1", $"file_{i}.txt", $"/Documents/file_{i}.txt", 100,
                DateTime.UtcNow, $"C:\\Sync\\Documents\\file_{i}.txt", null, null, $"hash_{i}",
                FileSyncStatus.PendingUpload, null));
        }

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\\Sync", true, null, null, false, false, 3, 50, null));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(filesToUpload);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        // Mock upload
        _ = mocks.GraphApiClient.UploadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(new Microsoft.Graph.Models.DriveItem
            {
                Id = $"uploaded_{Guid.CreateVersion7():N}",
                Name = callInfo.ArgAt<string>(1).Split('\\', '/').Last(),
                CTag = $"ctag_{Guid.CreateVersion7():N}",
                ETag = $"etag_{Guid.CreateVersion7():N}",
                LastModifiedDateTime = DateTimeOffset.UtcNow
            }));

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        // Should call SaveBatchAsync 3 times: 50, 50, 20
        await mocks.FileMetadataRepo.Received(3).SaveBatchAsync(Arg.Any<IEnumerable<FileMetadata>>(), Arg.Any<CancellationToken>());
        // Check batch sizes
        Received.InOrder(() =>
        {
            _ = mocks.FileMetadataRepo.SaveBatchAsync(Arg.Is<IEnumerable<FileMetadata>>(batch => batch.Count() == 50), Arg.Any<CancellationToken>());
            _ = mocks.FileMetadataRepo.SaveBatchAsync(Arg.Is<IEnumerable<FileMetadata>>(batch => batch.Count() == 50), Arg.Any<CancellationToken>());
            _ = mocks.FileMetadataRepo.SaveBatchAsync(Arg.Is<IEnumerable<FileMetadata>>(batch => batch.Count() == 20), Arg.Any<CancellationToken>());
        });
    }
    [Fact]
    public async Task DownloadFiles_BatchedDbUpdates_UsesSaveBatchAsync()
    {
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        var filesToDownload = new List<FileMetadata>();
        for(var i = 0; i < 120; i++)
        {
            filesToDownload.Add(new FileMetadata(
                $"id_{i}", "acc1", $"file_{i}.txt", $"/Documents/file_{i}.txt", 100,
                DateTime.UtcNow, $"C:\\Sync\\Documents\\file_{i}.txt", null, null, $"hash_{i}",
                FileSyncStatus.PendingDownload, null));
        }

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\\Sync", true, null, null, false, false, 3, 50, null));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((filesToDownload.AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        // Mock download and hash
        _ = mocks.GraphApiClient.DownloadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _ = mocks.LocalScanner.ComputeFileHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("hash_downloaded");

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        // Should call SaveBatchAsync 3 times: 50, 50, 20
        await mocks.FileMetadataRepo.Received(3).SaveBatchAsync(Arg.Any<IEnumerable<FileMetadata>>(), Arg.Any<CancellationToken>());
        // Check batch sizes
        Received.InOrder(() =>
        {
            _ = mocks.FileMetadataRepo.SaveBatchAsync(Arg.Is<IEnumerable<FileMetadata>>(batch => batch.Count() == 50), Arg.Any<CancellationToken>());
            _ = mocks.FileMetadataRepo.SaveBatchAsync(Arg.Is<IEnumerable<FileMetadata>>(batch => batch.Count() == 50), Arg.Any<CancellationToken>());
            _ = mocks.FileMetadataRepo.SaveBatchAsync(Arg.Is<IEnumerable<FileMetadata>>(batch => batch.Count() == 20), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task StartSyncAndReportProgress()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        progressStates.Count.ShouldBeGreaterThan(0);
        progressStates.Last().Status.ShouldBe(SyncStatus.Completed);
    }

    [Fact]
    public async Task UploadNewLocalFiles()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var localFile = new FileMetadata("", "acc1", "doc.txt", "/Documents/doc.txt", 100,
            DateTime.UtcNow, @"C:\Sync\Documents\doc.txt", null, null, "hash123",
            FileSyncStatus.PendingUpload, null);

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([localFile]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        // Verify UploadFileAsync was called
        _ = await mocks.GraphApiClient.Received(1).UploadFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IProgress<long>?>(),
            Arg.Any<CancellationToken>());

        // Verify AddAsync called with PendingUpload before upload
        await mocks.FileMetadataRepo.Received(1).AddAsync(
            Arg.Is<FileMetadata>(f => f.Name == "doc.txt" && f.SyncStatus == FileSyncStatus.PendingUpload),
            Arg.Any<CancellationToken>());

        // Verify UpdateAsync called with Synced after upload
        await mocks.FileMetadataRepo.Received(1).UpdateAsync(
            Arg.Is<FileMetadata>(f => f.Name == "doc.txt" && f.SyncStatus == FileSyncStatus.Synced),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SkipUnchangedFiles()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var localFile = new FileMetadata("file1", "acc1", "doc.txt", "/Documents/doc.txt", 100,
            DateTime.UtcNow, @"C:\Sync\Documents\doc.txt", null, null, "hash123",
            FileSyncStatus.Synced, null);

        // Remote file with same metadata (unchanged)
        FileMetadata remoteFile = localFile with { };

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([localFile]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata> { remoteFile }.AsReadOnly(), "delta_123")); // Include remote file
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([localFile]);

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        await mocks.FileMetadataRepo.DidNotReceive().AddAsync(Arg.Any<FileMetadata>(), Arg.Any<CancellationToken>());
        await mocks.FileMetadataRepo.DidNotReceive().UpdateAsync(Arg.Any<FileMetadata>(), Arg.Any<CancellationToken>());
        await mocks.FileMetadataRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleNoSelectedFolders()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        progressStates.Last().Status.ShouldBe(SyncStatus.Idle);
    }

    [Fact]
    public async Task HandleAccountNotFound()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns((AccountInfo?)null);

        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        progressStates.Last().Status.ShouldBe(SyncStatus.Failed);
    }

    [Fact]
    public async Task HandleCancellation()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));

        using var cts = new CancellationTokenSource();
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<FileMetadata>>(new OperationCanceledException(cts.Token)));
        await cts.CancelAsync();

        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        _ = await Should.ThrowAsync<OperationCanceledException>(async () =>
            await engine.StartSyncAsync("acc1", cts.Token));

        progressStates.Last().Status.ShouldBe(SyncStatus.Paused);
    }

    [Fact]
    public async Task ReportProgressWithFileCountsAndBytes()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        FileMetadata[] files =
        [
            new FileMetadata("", "acc1", "file1.txt", "/Documents/file1.txt", 1000,
                DateTime.UtcNow, @"C:\Sync\Documents\file1.txt", null, null, "hash1",
                FileSyncStatus.PendingUpload, null),
            new FileMetadata("", "acc1", "file2.txt", "/Documents/file2.txt", 2000,
                DateTime.UtcNow, @"C:\Sync\Documents\file2.txt", null, null, "hash2",
                FileSyncStatus.PendingUpload, null)
        ];

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(files);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        SyncState finalState = progressStates.Last();
        finalState.TotalFiles.ShouldBe(2);
        finalState.CompletedFiles.ShouldBe(2);
        finalState.TotalBytes.ShouldBe(3000);
        finalState.CompletedBytes.ShouldBe(3000);
    }

    [Fact]
    public async Task DownloadNewRemoteFiles()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var remoteFile = new FileMetadata("remote1", "acc1", "report.pdf", "/Documents/report.pdf", 500,
            DateTime.UtcNow, string.Empty, "ctag123", "etag456", null,
            FileSyncStatus.PendingDownload, SyncDirection.Download);

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata> { remoteFile }.AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        await mocks.FileMetadataRepo.Received(1).AddAsync(
            Arg.Is<FileMetadata>(f => f.Name == "report.pdf" && f.SyncStatus == FileSyncStatus.Synced && f.LastSyncDirection == SyncDirection.Download),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleRemoteDeletions()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var deletedFile = new FileMetadata("deleted1", "acc1", "old.txt", "/Documents/old.txt", 100,
            DateTime.UtcNow.AddDays(-5), string.Empty, "ctag", "etag", "hash",
            FileSyncStatus.Synced, SyncDirection.Download);

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([deletedFile]);

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        await mocks.FileMetadataRepo.Received(1).DeleteAsync("deleted1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PerformBidirectionalSync()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var localFile = new FileMetadata("", "acc1", "upload.txt", "/Documents/upload.txt", 100,
            DateTime.UtcNow, @"C:\Sync\Documents\upload.txt", null, null, "uploadhash",
            FileSyncStatus.PendingUpload, null);
        var remoteFile = new FileMetadata("remote1", "acc1", "download.pdf", "/Documents/download.pdf", 200,
            DateTime.UtcNow, string.Empty, "ctag", "etag", null,
            FileSyncStatus.PendingDownload, SyncDirection.Download);

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([localFile]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata> { remoteFile }.AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        // Expect 2 AddAsync: PendingUpload for upload, Synced for download
        await mocks.FileMetadataRepo.Received(2).AddAsync(Arg.Any<FileMetadata>(), Arg.Any<CancellationToken>());
        // Expect 1 UpdateAsync: Synced for upload completion
        await mocks.FileMetadataRepo.Received(1).UpdateAsync(Arg.Any<FileMetadata>(), Arg.Any<CancellationToken>());

        SyncState finalState = progressStates.Last();
        finalState.Status.ShouldBe(SyncStatus.Completed);
        finalState.TotalFiles.ShouldBe(2);
        finalState.CompletedFiles.ShouldBe(2);
        finalState.TotalBytes.ShouldBe(300);
    }

    [Fact]
    public async Task DetectConflictWhenBothFilesModified()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        DateTime baseTime = DateTime.UtcNow;

        var localFile = new FileMetadata("file1", "acc1", "conflict.txt", "/Documents/conflict.txt", 150,
            baseTime.AddMinutes(5), @"C:\Sync\Documents\conflict.txt", null, null, "localhash",
            FileSyncStatus.PendingUpload, null);
        var remoteFile = new FileMetadata("file1", "acc1", "conflict.txt", "/Documents/conflict.txt", 200,
            baseTime.AddMinutes(3), string.Empty, "newctag", "newetag", null,
            FileSyncStatus.PendingDownload, SyncDirection.Download);
        var existingFile = new FileMetadata("file1", "acc1", "conflict.txt", "/Documents/conflict.txt", 100,
            baseTime, @"C:\Sync\Documents\conflict.txt", "oldctag", "oldetag", "oldhash",
            FileSyncStatus.Synced, SyncDirection.Upload);

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([localFile]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata> { remoteFile }.AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([existingFile]);

        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        SyncState finalState = progressStates.Last();
        finalState.ConflictsDetected.ShouldBe(1);
    }

    [Fact]
    public async Task RespectMaxParallelUpDownloadsSettingForUploads()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();

        // Create multiple files to upload
        var files = Enumerable.Range(1, 5)
            .Select(i => new FileMetadata(
                "", "acc1", $"file{i}.txt", $"/Documents/file{i}.txt", 100,
                DateTime.UtcNow, $@"C:\Sync\Documents\file{i}.txt", null, null, $"hash{i}",
                FileSyncStatus.PendingUpload, null))
            .ToList();

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);

        // Set MaxParallelUpDownloads to 2
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 2, 50, null));

        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(files);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        // Verify all files were uploaded
        _ = await mocks.GraphApiClient.Received(5).UploadFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IProgress<long>?>(),
            Arg.Any<CancellationToken>());

        // Check that progress reported uploads (filesUploading should have been > 0 at some point)
        var uploadingStates = progressStates.Where(s => s.FilesUploading > 0).ToList();
        uploadingStates.ShouldNotBeEmpty();

        // The max concurrent uploads should not exceed the configured limit
        // (Though in unit tests with mocks, this happens so fast we might not catch it,
        // but the implementation should honor the semaphore limit)
        uploadingStates.Max(s => s.FilesUploading).ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task RespectMaxParallelUpDownloadsSettingForDownloads()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();

        // Create multiple files to download
        var files = Enumerable.Range(1, 5)
            .Select(i => new FileMetadata(
                $"remote{i}", "acc1", $"file{i}.txt", $"/Documents/file{i}.txt", 100,
                DateTime.UtcNow, $@"C:\Sync\Documents\file{i}.txt", $"ctag{i}", $"etag{i}", null,
                FileSyncStatus.PendingDownload, null))
            .ToList();

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);

        // Set MaxParallelUpDownloads to 3
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));

        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((files.AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        // Mock hash computation
        _ = mocks.LocalScanner.ComputeFileHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("computed_hash");

        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        // Verify all files were downloaded
        await mocks.GraphApiClient.Received(5).DownloadFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        // Check that progress reported downloads (filesDownloading should have been > 0 at some point)
        var downloadingStates = progressStates.Where(s => s.FilesDownloading > 0).ToList();
        downloadingStates.ShouldNotBeEmpty();

        // The max concurrent downloads should not exceed the configured limit
        downloadingStates.Max(s => s.FilesDownloading).ShouldBeLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task StopSyncAsyncCancelsPendingSync()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));

        var tcs = new TaskCompletionSource<IReadOnlyList<FileMetadata>>();
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        _ = engine.StartSyncAsync("acc1", CancellationToken.None);

        await Task.Delay(100, CancellationToken.None); // Give sync time to start

        await engine.StopSyncAsync();

        _ = tcs.TrySetCanceled();
    }

    [Fact]
    public async Task GetConflictsAsyncReturnsUnresolvedConflicts()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var conflict1 = new SyncConflict(
            Id: "conflict1",
            AccountId: "acc1",
            FilePath: "/Documents/conflict.txt",
            LocalModifiedUtc: DateTime.UtcNow.AddMinutes(-5),
            RemoteModifiedUtc: DateTime.UtcNow.AddMinutes(-3),
            LocalSize: 150,
            RemoteSize: 200,
            DetectedUtc: DateTime.UtcNow,
            ResolutionStrategy: ConflictResolutionStrategy.None,
            IsResolved: false);

        var conflicts = new List<SyncConflict> { conflict1 };
        _ = mocks.SyncConflictRepo.GetUnresolvedByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(conflicts);

        IReadOnlyList<SyncConflict> result = await engine.GetConflictsAsync("acc1", CancellationToken.None);

        _ = result.ShouldHaveSingleItem();
        result[0].FilePath.ShouldBe("/Documents/conflict.txt");
    }

    [Fact]
    public async Task PreventConcurrentSyncAttempts()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));

        var tcs = new TaskCompletionSource<IReadOnlyList<FileMetadata>>();
        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        _ = engine.StartSyncAsync("acc1", CancellationToken.None);

        await Task.Delay(100, CancellationToken.None); // First sync starts

        // Second sync attempt should return immediately without executing
        await engine.StartSyncAsync("acc1", CancellationToken.None);

        // Only one scan call should have been made
        _ = await mocks.LocalScanner.Received(1).ScanFolderAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleExceptionDuringSyncAndReportFailed()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));

        _ = mocks.LocalScanner.ScanFolderAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<FileMetadata>>(new InvalidOperationException("Test exception")));

        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        _ = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await engine.StartSyncAsync("acc1", CancellationToken.None));

        progressStates.Last().Status.ShouldBe(SyncStatus.Failed);
    }

    [Theory]
    [InlineData("/drives/abc123/root:/Documents", "/Documents")]
    [InlineData("/drive/root:/MyFolder", "/MyFolder")]
    [InlineData("/Documents/Subfolder", "/Documents/Subfolder")]
    [InlineData(null, null)]
    [InlineData("/", "/")]
    public void FormatScanningFolderForDisplayRemovesGraphApiPrefixes(string? input, string? expected)
    {
        var result = SyncEngine.FormatScanningFolderForDisplay(input);

        if(expected is not null)
        {
            result.ShouldBe($"OneDrive: {expected}");
        }
        else
        {
            result.ShouldBe(expected);
        }
    }

    [Fact]
    public async Task HandleEmptyFilesCorrectly()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var emptyFile = new FileMetadata("", "acc1", "empty.txt", "/Documents/empty.txt", 0,
            DateTime.UtcNow, @"C:\Sync\Documents\empty.txt", null, null, "emptyhash",
            FileSyncStatus.PendingUpload, null);

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));

        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([emptyFile]);

        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));

        _ = mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        // Empty file should still be uploaded
        _ = await mocks.GraphApiClient.Received(1).UploadFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IProgress<long>?>(),
            Arg.Any<CancellationToken>());

        progressStates.Last().TotalBytes.ShouldBe(0);
        progressStates.Last().CompletedBytes.ShouldBe(0);
    }

    [Fact]
    public async Task HandleFileModifiedWithoutHashChange()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        DateTime baseTime = DateTime.UtcNow;

        var localFile = new FileMetadata("file1", "acc1", "samehash.txt", "/Documents/samehash.txt", 100,
            baseTime.AddMinutes(10), @"C:\Sync\Documents\samehash.txt", null, null, "samehash",
            FileSyncStatus.PendingUpload, null);

        var existingFile = new FileMetadata("file1", "acc1", "samehash.txt", "/Documents/samehash.txt", 100,
            baseTime, @"C:\Sync\Documents\samehash.txt", "oldctag", "oldetag", "samehash",
            FileSyncStatus.Synced, SyncDirection.Upload);

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));

        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([localFile]);

        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata> { existingFile }.AsReadOnly(), "delta_123"));

        _ = mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([existingFile]);

        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        // File has same hash, so should not be uploaded
        _ = await mocks.GraphApiClient.DidNotReceive().UploadFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IProgress<long>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleUploadFailureAndMarkFileAsFailed()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var fileToUpload = new FileMetadata("", "acc1", "upload.txt", "/Documents/upload.txt", 100,
            DateTime.UtcNow, @"C:\Sync\Documents\upload.txt", null, null, "hash123",
            FileSyncStatus.PendingUpload, null);

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));

        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([fileToUpload]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        // Mock upload to throw exception
        _ = mocks.GraphApiClient.UploadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<long>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Microsoft.Graph.Models.DriveItem>(new HttpRequestException("Upload failed")));

        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        // Verify file was marked as Failed in database
        await mocks.FileMetadataRepo.Received(1).AddAsync(
            Arg.Is<FileMetadata>(f => f.Name == "upload.txt" && f.SyncStatus == FileSyncStatus.Failed),
            Arg.Any<CancellationToken>());

        progressStates.Last().Status.ShouldBe(SyncStatus.Completed);
    }

    [Fact]
    public async Task HandleDownloadFailureAndMarkFileAsFailed()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var remoteFile = new FileMetadata("remote1", "acc1", "download.pdf", "/Documents/download.pdf", 500,
            DateTime.UtcNow, string.Empty, "ctag123", "etag456", null,
            FileSyncStatus.PendingDownload, SyncDirection.Download);

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));

        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata> { remoteFile }.AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        // Mock download to throw exception
        _ = mocks.GraphApiClient.DownloadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HttpRequestException("Download failed")));

        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        // Verify file was marked as Failed in database
        await mocks.FileMetadataRepo.Received(1).AddAsync(
            Arg.Is<FileMetadata>(f => f.Name == "download.pdf" && f.SyncStatus == FileSyncStatus.Failed),
            Arg.Any<CancellationToken>());

        progressStates.Last().Status.ShouldBe(SyncStatus.Completed);
    }

    [Fact]
    public async Task HandleLargeFilesWithAccurateByteTracking()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        var largeFile = new FileMetadata("", "acc1", "large.iso", "/Documents/large.iso", 1024 * 1024 * 500,
            DateTime.UtcNow, @"C:\Sync\Documents\large.iso", null, null, "bighash",
            FileSyncStatus.PendingUpload, null);

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 1, 50, null));

        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([largeFile]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata>().AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        SyncState finalState = progressStates.Last();
        finalState.TotalBytes.ShouldBe(1024 * 1024 * 500);
        finalState.CompletedBytes.ShouldBe(1024 * 1024 * 500);
        finalState.TotalFiles.ShouldBe(1);
    }

    [Fact]
    public async Task HandleMultipleFilesWithMixedOperations()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();

        FileMetadata[] filesToUpload =
        [
            new FileMetadata("", "acc1", "new1.txt", "/Docs/new1.txt", 100, DateTime.UtcNow, @"C:\Sync\Docs\new1.txt", null, null, "hash1", FileSyncStatus.PendingUpload, null),
            new FileMetadata("", "acc1", "new2.txt", "/Docs/new2.txt", 200, DateTime.UtcNow, @"C:\Sync\Docs\new2.txt", null, null, "hash2", FileSyncStatus.PendingUpload, null),
        ];

        FileMetadata[] filesToDownload =
        [
            new FileMetadata("rem1", "acc1", "remote1.txt", "/Docs/remote1.txt", 150, DateTime.UtcNow, "", "ctag1", "etag1", null, FileSyncStatus.PendingDownload, SyncDirection.Download),
            new FileMetadata("rem2", "acc1", "remote2.txt", "/Docs/remote2.txt", 250, DateTime.UtcNow, "", "ctag2", "etag2", null, FileSyncStatus.PendingDownload, SyncDirection.Download),
        ];

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Docs"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 2, 50, null));

        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(filesToUpload.ToList());
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((filesToDownload.AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([]);

        _ = mocks.LocalScanner.ComputeFileHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("computed_hash");

        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        SyncState finalState = progressStates.Last();
        finalState.Status.ShouldBe(SyncStatus.Completed);
        finalState.TotalFiles.ShouldBe(4);
        finalState.TotalBytes.ShouldBe(100 + 200 + 150 + 250);
        finalState.CompletedFiles.ShouldBe(4);
    }

    [Fact]
    public async Task DetectConflictWithExistingUnresolvedConflict()
    {
        (SyncEngine? engine, TestMocks? mocks) = CreateTestEngine();
        DateTime baseTime = DateTime.UtcNow;

        var localFile = new FileMetadata("file1", "acc1", "conflict.txt", "/Documents/conflict.txt", 150,
            baseTime.AddMinutes(10), @"C:\Sync\Documents\conflict.txt", null, null, "localhash",
            FileSyncStatus.PendingUpload, null);
        var remoteFile = new FileMetadata("file1", "acc1", "conflict.txt", "/Documents/conflict.txt", 200,
            baseTime.AddMinutes(5), string.Empty, "newctag", "newetag", null,
            FileSyncStatus.PendingDownload, SyncDirection.Download);
        var existingFile = new FileMetadata("file1", "acc1", "conflict.txt", "/Documents/conflict.txt", 100,
            baseTime, @"C:\Sync\Documents\conflict.txt", "oldctag", "oldetag", "oldhash",
            FileSyncStatus.Synced, SyncDirection.Upload);

        var existingConflict = new SyncConflict(
            Id: "conflict1",
            AccountId: "acc1",
            FilePath: "/Documents/conflict.txt",
            LocalModifiedUtc: baseTime.AddHours(-1),
            RemoteModifiedUtc: baseTime.AddHours(-2),
            LocalSize: 100,
            RemoteSize: 100,
            DetectedUtc: baseTime.AddHours(-1),
            ResolutionStrategy: ConflictResolutionStrategy.None,
            IsResolved: false);

        _ = mocks.SyncConfigRepo.GetSelectedFoldersAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(["/Documents"]);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, null));

        _ = mocks.LocalScanner.ScanFolderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([localFile]);
        _ = mocks.RemoteDetector.DetectChangesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileMetadata> { remoteFile }.AsReadOnly(), "delta_123"));
        _ = mocks.FileMetadataRepo.GetByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns([existingFile]);

        _ = mocks.SyncConflictRepo.GetByFilePathAsync("acc1", "/Documents/conflict.txt", Arg.Any<CancellationToken>())
            .Returns(existingConflict);

        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", CancellationToken.None);

        SyncState finalState = progressStates.Last();
        finalState.ConflictsDetected.ShouldBe(1);
    }

    private static (SyncEngine Engine, TestMocks Mocks) CreateTestEngine()
    {
        ILocalFileScanner localScanner = Substitute.For<ILocalFileScanner>();
        IRemoteChangeDetector remoteDetector = Substitute.For<IRemoteChangeDetector>();
        IFileMetadataRepository fileMetadataRepo = Substitute.For<IFileMetadataRepository>();
        ISyncConfigurationRepository syncConfigRepo = Substitute.For<ISyncConfigurationRepository>();
        IAccountRepository accountRepo = Substitute.For<IAccountRepository>();
        IGraphApiClient graphApiClient = Substitute.For<IGraphApiClient>();
        ISyncConflictRepository syncConflictRepo = Substitute.For<ISyncConflictRepository>();

        // Setup default mock return for UploadFileAsync to prevent null reference exceptions
        _ = graphApiClient.UploadFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IProgress<long>?>(),
            Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(new Microsoft.Graph.Models.DriveItem
            {
                Id = $"uploaded_{Guid.CreateVersion7():N}",
                Name = callInfo.ArgAt<string>(1).Split('\\', '/').Last(),
                CTag = $"ctag_{Guid.CreateVersion7():N}",
                ETag = $"etag_{Guid.CreateVersion7():N}",
                LastModifiedDateTime = DateTimeOffset.UtcNow
            }));

        // Setup default mock for GetByFilePathAsync to return null (no existing conflict)
        _ = syncConflictRepo.GetByFilePathAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns((SyncConflict?)null);

        ISyncSessionLogRepository syncSessionLogRepo = Substitute.For<ISyncSessionLogRepository>();
        IFileOperationLogRepository fileOperationLogRepo = Substitute.For<IFileOperationLogRepository>();

        var engine = new SyncEngine(localScanner, remoteDetector, fileMetadataRepo, syncConfigRepo, accountRepo, graphApiClient, syncConflictRepo, syncSessionLogRepo, fileOperationLogRepo);
        var mocks = new TestMocks(localScanner, remoteDetector, fileMetadataRepo, syncConfigRepo, accountRepo, graphApiClient, syncConflictRepo);

        return (engine, mocks);
    }

    private sealed record TestMocks(
        ILocalFileScanner LocalScanner,
        IRemoteChangeDetector RemoteDetector,
        IFileMetadataRepository FileMetadataRepo,
        ISyncConfigurationRepository SyncConfigRepo,
        IAccountRepository AccountRepo,
        IGraphApiClient GraphApiClient,
        ISyncConflictRepository SyncConflictRepo);
}
