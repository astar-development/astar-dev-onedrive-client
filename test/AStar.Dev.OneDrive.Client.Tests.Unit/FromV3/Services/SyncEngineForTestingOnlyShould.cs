using AStar.Dev.OneDrive.Client.FromV3;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Models.Enums;
using AStar.Dev.OneDrive.Client.FromV3.OneDriveServices;
using AStar.Dev.OneDrive.Client.FromV3.Repositories;
using AStar.Dev.OneDrive.Client.Services;
using ILocalFileScanner = AStar.Dev.OneDrive.Client.FromV3.ILocalFileScanner;
using SyncEngine = AStar.Dev.OneDrive.Client.FromV3.SyncEngine;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.FromV3.Services;

public sealed class SyncEngineForTestingOnlyShould
{
    [Fact]
    public async Task UpdateTheUiWhenReportingProgressForTheInitialSyncStart()
    {
        SyncEngine syncEngine = CreateSyncEngine();
        var progressUpdates = new List<SyncState>();

        _ = syncEngine.Progress.Subscribe(progressUpdates.Add);
        await Task.Delay(100, CancellationToken.None); // allow time for initial progress state to be emitted

        syncEngine.ReportProgress(string.Empty, SyncStatus.Running);

        SyncState finalProgress = progressUpdates.Last();
        finalProgress.CompletedBytes.ShouldBe(0);
        finalProgress.TotalBytes.ShouldBe(0);
        finalProgress.TotalFiles.ShouldBe(0);
        finalProgress.CompletedFiles.ShouldBe(0);
        finalProgress.EstimatedSecondsRemaining.ShouldBe(null);
        finalProgress.MegabytesPerSecond.ShouldBe(0);
        finalProgress.FilesDeleted.ShouldBe(0);
        finalProgress.FilesDownloading.ShouldBe(0);
        finalProgress.FilesUploading.ShouldBe(0);
        finalProgress.CurrentScanningFolder.ShouldBe(null);
        finalProgress.Status.ShouldBe(SyncStatus.Running);
    }

    [Fact]
    public async Task UpdateTheUiWhenReportingProgressForDownload()
    {
        SyncEngine syncEngine = CreateSyncEngine();
        var progressUpdates = new List<SyncState>();

        _ = syncEngine.Progress.Subscribe(progressUpdates.Add);
        await Task.Delay(100, CancellationToken.None); // allow time for initial progress state to be emitted

        syncEngine.ReportProgress(string.Empty, SyncStatus.Running, 21, 2, 1361359, 869673, 3, 0, 0, 2, null, 1361359);

        SyncState finalProgress = progressUpdates.Last();
        finalProgress.CompletedBytes.ShouldBe(869673);
        finalProgress.TotalBytes.ShouldBe(1361359);
        finalProgress.TotalFiles.ShouldBe(21);
        finalProgress.CompletedFiles.ShouldBe(2);
        finalProgress.EstimatedSecondsRemaining.ShouldBe(1);
        finalProgress.MegabytesPerSecond.ShouldBeGreaterThan(7);
        finalProgress.FilesDeleted.ShouldBe(0);
        finalProgress.FilesDownloading.ShouldBe(3);
        finalProgress.FilesUploading.ShouldBe(0);
        finalProgress.CurrentScanningFolder.ShouldBe(null);
        finalProgress.Status.ShouldBe(SyncStatus.Running);
    }

    [Fact]
    public async Task UpdateTheUiWhenReportingProgressForDownloadLaterInTheProcess()
    {
        SyncEngine syncEngine = CreateSyncEngine();
        var progressUpdates = new List<SyncState>();

        _ = syncEngine.Progress.Subscribe(progressUpdates.Add);
        await Task.Delay(100, CancellationToken.None); // allow time for initial progress state to be emitted

        syncEngine.ReportProgress(string.Empty, SyncStatus.Running, 21, 20, 1361359, 1209570, 2, 0, 0, 2, null, 1361359);

        SyncState finalProgress = progressUpdates.Last();
        finalProgress.CompletedBytes.ShouldBe(1209570);
        finalProgress.TotalBytes.ShouldBe(1361359);
        finalProgress.TotalFiles.ShouldBe(21);
        finalProgress.CompletedFiles.ShouldBe(20);
        finalProgress.EstimatedSecondsRemaining.ShouldBe(1);
        finalProgress.MegabytesPerSecond.ShouldBeGreaterThan(0);
        finalProgress.FilesDeleted.ShouldBe(0);
        finalProgress.FilesDownloading.ShouldBe(2);
        finalProgress.FilesUploading.ShouldBe(0);
        finalProgress.CurrentScanningFolder.ShouldBe(null);
        finalProgress.Status.ShouldBe(SyncStatus.Running);
    }

    private static SyncEngine CreateSyncEngine()
    {
        ISyncConfigurationRepository syncConfigurationRepository = Substitute.For<ISyncConfigurationRepository>();
        IRemoteChangeDetector remoteChangeDetector = Substitute.For<IRemoteChangeDetector>();
        ILocalFileScanner localFileScanner = Substitute.For<ILocalFileScanner>();
        IFileMetadataRepository fileMetadataRepository = Substitute.For<IFileMetadataRepository>();
        IAccountRepository accountRepository = Substitute.For<IAccountRepository>();
        IGraphApiClient graphApiClient = Substitute.For<IGraphApiClient>();
        ISyncConflictRepository syncConflictRepository = Substitute.For<ISyncConflictRepository>();
        ISyncSessionLogRepository syncSessionLogRepository = Substitute.For<ISyncSessionLogRepository>();
        IFileOperationLogRepository fileOperationLogRepository = Substitute.For<IFileOperationLogRepository>();

        var syncEngine = new SyncEngine(localFileScanner, remoteChangeDetector, fileMetadataRepository, syncConfigurationRepository, accountRepository, graphApiClient, syncConflictRepository, syncSessionLogRepository, fileOperationLogRepository);

        return syncEngine;
    }
}
