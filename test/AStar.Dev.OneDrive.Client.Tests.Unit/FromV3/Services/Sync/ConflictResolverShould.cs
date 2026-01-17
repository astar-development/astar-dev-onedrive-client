using AStar.Dev.OneDrive.Client.FromV3;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Models.Enums;
using AStar.Dev.OneDrive.Client.FromV3.OneDriveServices;
using AStar.Dev.OneDrive.Client.FromV3.Repositories;
using AStar.Dev.OneDrive.Client.FromV3.Sync;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.FromV3.Services.Sync;

public sealed class ConflictResolverShould
{
    private readonly IGraphApiClient _graphApiClient = Substitute.For<IGraphApiClient>();
    private readonly IFileMetadataRepository _metadataRepo = Substitute.For<IFileMetadataRepository>();
    private readonly IAccountRepository _accountRepo = Substitute.For<IAccountRepository>();
    private readonly ISyncConflictRepository _conflictRepo = Substitute.For<ISyncConflictRepository>();
    private readonly ILocalFileScanner _localFileScanner = Substitute.For<ILocalFileScanner>();
    private readonly ILogger<ConflictResolver> _logger = Substitute.For<ILogger<ConflictResolver>>();

    [Fact]
    public void ThrowArgumentNullExceptionWhenConflictIsNull()
    {
        ConflictResolver resolver = CreateResolver();

        ArgumentNullException exception = Should.Throw<ArgumentNullException>(async () =>
            await resolver.ResolveAsync(null!, ConflictResolutionStrategy.KeepLocal, CancellationToken.None));

        exception.ParamName.ShouldBe("conflict");
    }

    [Fact]
    public async Task ThrowInvalidOperationExceptionWhenAccountNotFound()
    {
        ConflictResolver resolver = CreateResolver();
        SyncConflict conflict = CreateTestConflict();

        _ = _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns((AccountInfo?)null);

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepLocal, CancellationToken.None));

        exception.Message.ShouldContain("Account not found");
    }

    [Fact]
    public async Task KeepLocalVersionByUploadingLocalFile()
    {
        ConflictResolver resolver = CreateResolver();
        SyncConflict conflict = CreateTestConflict();
        AccountInfo account = CreateTestAccount();
        FileMetadata metadata = CreateTestMetadata(account.AccountId, conflict.FilePath);
        var localPath = Path.Combine(account.LocalSyncPath, conflict.FilePath);

        _ = _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _ = _metadataRepo.GetByPathAsync(account.AccountId, conflict.FilePath, Arg.Any<CancellationToken>())
            .Returns(metadata);

        // Create temporary test file
        _ = Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        await File.WriteAllTextAsync(localPath, "local content", CancellationToken.None);

        try
        {
            await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepLocal, CancellationToken.None);
            _ = await _graphApiClient.Received(1).UploadFileAsync(
                account.AccountId,
                localPath,
                conflict.FilePath,
                Arg.Any<IProgress<long>?>(),
                Arg.Any<CancellationToken>());

            await _metadataRepo.Received(1).UpdateAsync(
                Arg.Is<FileMetadata>(m =>
                    m.Id == metadata.Id &&
                    m.SyncStatus == FileSyncStatus.Synced &&
                    m.LastSyncDirection == SyncDirection.Upload),
                Arg.Any<CancellationToken>());

            await _conflictRepo.Received(1).UpdateAsync(
                Arg.Is<SyncConflict>(c =>
                    c.Id == conflict.Id &&
                    c.IsResolved &&
                    c.ResolutionStrategy == ConflictResolutionStrategy.KeepLocal),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            File.Delete(localPath);
            Directory.Delete(account.LocalSyncPath, true);
        }
    }

    [Fact]
    public async Task ThrowFileNotFoundExceptionWhenKeepLocalAndFileDoesNotExist()
    {
        ConflictResolver resolver = CreateResolver();
        SyncConflict conflict = CreateTestConflict();
        AccountInfo account = CreateTestAccount();
        FileMetadata metadata = CreateTestMetadata(account.AccountId, conflict.FilePath);

        _ = _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _ = _metadataRepo.GetByPathAsync(account.AccountId, conflict.FilePath, Arg.Any<CancellationToken>())
            .Returns(metadata);

        FileNotFoundException exception = await Should.ThrowAsync<FileNotFoundException>(async () =>
            await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepLocal, CancellationToken.None));

        exception.Message.ShouldContain("Local file not found");
    }

    [Fact]
    public async Task KeepRemoteVersionByDownloadingRemoteFile()
    {
        ConflictResolver resolver = CreateResolver();
        SyncConflict conflict = CreateTestConflict();
        AccountInfo account = CreateTestAccount();
        FileMetadata metadata = CreateTestMetadata(account.AccountId, conflict.FilePath);
        var localPath = Path.Combine(account.LocalSyncPath, conflict.FilePath);

        _ = _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _ = _metadataRepo.GetByPathAsync(account.AccountId, conflict.FilePath, Arg.Any<CancellationToken>())
            .Returns(metadata);

        // Ensure directory exists
        _ = Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

        // Mock the download to create the file
        _ = _graphApiClient.DownloadFileAsync(
            account.AccountId,
            metadata.Id,
            localPath,
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => File.WriteAllText(localPath, "remote content"));

        try
        {
            await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepRemote, CancellationToken.None);

            await _graphApiClient.Received(1).DownloadFileAsync(
                account.AccountId,
                metadata.Id,
                localPath,
                Arg.Any<CancellationToken>());

            await _metadataRepo.Received(1).UpdateAsync(
                Arg.Is<FileMetadata>(m =>
                    m.Id == metadata.Id &&
                    m.SyncStatus == FileSyncStatus.Synced &&
                    m.LastSyncDirection == SyncDirection.Download),
                Arg.Any<CancellationToken>());

            await _conflictRepo.Received(1).UpdateAsync(
                Arg.Is<SyncConflict>(c =>
                    c.Id == conflict.Id &&
                    c.IsResolved &&
                    c.ResolutionStrategy == ConflictResolutionStrategy.KeepRemote),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            if(File.Exists(localPath))
            {
                File.Delete(localPath);
            }

            Directory.Delete(account.LocalSyncPath, true);
        }
    }

    [Fact]
    public async Task KeepBothVersionsByRenamingLocalAndDownloadingRemote()
    {
        ConflictResolver resolver = CreateResolver();
        SyncConflict conflict = CreateTestConflict();
        AccountInfo account = CreateTestAccount();
        FileMetadata metadata = CreateTestMetadata(account.AccountId, conflict.FilePath);
        var localPath = Path.Combine(account.LocalSyncPath, conflict.FilePath);

        _ = _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _ = _metadataRepo.GetByPathAsync(account.AccountId, conflict.FilePath, Arg.Any<CancellationToken>())
            .Returns(metadata);

        // Create temporary test file
        _ = Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        await File.WriteAllTextAsync(localPath, "local content", CancellationToken.None);

        // Mock the download to create the file
        _ = _graphApiClient.DownloadFileAsync(
            account.AccountId,
            metadata.Id,
            localPath,
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => File.WriteAllText(localPath, "remote content"));

        // Mock GetDriveItemAsync to return remote file metadata
        var remoteItem = new Microsoft.Graph.Models.DriveItem
        {
            Id = metadata.Id,
            Name = "test.txt",
            Size = 14, // "remote content" length
            LastModifiedDateTime = DateTime.UtcNow,
            CTag = "remote-ctag",
            ETag = "remote-etag"
        };
        _ = _graphApiClient.GetDriveItemAsync(account.AccountId, metadata.Id, Arg.Any<CancellationToken>())
            .Returns(remoteItem);

        // Mock ComputeFileHashAsync for both downloaded and conflict files
        _ = _localFileScanner.ComputeFileHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("computed-hash-123");

        try
        {
            await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepBoth, CancellationToken.None);

            // Verify original file now has remote content
            File.Exists(localPath).ShouldBeTrue();
            (await File.ReadAllTextAsync(localPath, CancellationToken.None)).ShouldBe("remote content");
            // Verify conflict file exists with local content
            var directory = Path.GetDirectoryName(localPath)!;
            var conflictFiles = Directory.GetFiles(directory, "*Conflict*.txt");
            conflictFiles.Length.ShouldBe(1);
            (await File.ReadAllTextAsync(conflictFiles[0], CancellationToken.None)).ShouldBe("local content");

            await _graphApiClient.Received(1).DownloadFileAsync(
                account.AccountId,
                metadata.Id,
                localPath,
                Arg.Any<CancellationToken>());

            _ = await _graphApiClient.Received(1).GetDriveItemAsync(
                account.AccountId,
                metadata.Id,
                Arg.Any<CancellationToken>());

            _ = await _localFileScanner.Received(1).ComputeFileHashAsync(
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());

            await _metadataRepo.Received(1).UpdateAsync(
                Arg.Is<FileMetadata>(m =>
                    m.Id == metadata.Id &&
                    m.SyncStatus == FileSyncStatus.Synced &&
                    m.LastSyncDirection == SyncDirection.Download &&
                    m.LocalHash == "computed-hash-123"),
                Arg.Any<CancellationToken>());

            await _conflictRepo.Received(1).UpdateAsync(
                Arg.Is<SyncConflict>(c =>
                    c.Id == conflict.Id &&
                    c.IsResolved &&
                    c.ResolutionStrategy == ConflictResolutionStrategy.KeepBoth),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            Directory.Delete(account.LocalSyncPath, true);
        }
    }

    [Fact]
    public async Task ThrowInvalidOperationExceptionWhenMetadataNotFoundForKeepLocal()
    {
        ConflictResolver resolver = CreateResolver();
        SyncConflict conflict = CreateTestConflict();
        AccountInfo account = CreateTestAccount();
        var localPath = Path.Combine(account.LocalSyncPath, conflict.FilePath);

        _ = _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);
        _ = _metadataRepo.GetByPathAsync(account.AccountId, conflict.FilePath, Arg.Any<CancellationToken>())
            .Returns((FileMetadata?)null);

        // Create temporary test file
        _ = Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        await File.WriteAllTextAsync(localPath, "local content", CancellationToken.None);

        try
        {
            InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.KeepLocal, CancellationToken.None));
            exception.Message.ShouldContain("File metadata not found");
        }
        finally
        {
            File.Delete(localPath);
            Directory.Delete(account.LocalSyncPath, true);
        }
    }

    [Fact]
    public async Task SkipResolutionWhenStrategyIsNone()
    {
        ConflictResolver resolver = CreateResolver();
        SyncConflict conflict = CreateTestConflict();
        AccountInfo account = CreateTestAccount();

        _ = _accountRepo.GetByIdAsync(conflict.AccountId, Arg.Any<CancellationToken>())
            .Returns(account);

        await resolver.ResolveAsync(conflict, ConflictResolutionStrategy.None, CancellationToken.None);

        // Verify no API calls were made
        _ = await _graphApiClient.DidNotReceive().UploadFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IProgress<long>?>(),
            Arg.Any<CancellationToken>());
        await _graphApiClient.DidNotReceive().DownloadFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await _metadataRepo.DidNotReceive().UpdateAsync(
            Arg.Any<FileMetadata>(),
            Arg.Any<CancellationToken>());
        await _conflictRepo.DidNotReceive().UpdateAsync(
            Arg.Any<SyncConflict>(),
            Arg.Any<CancellationToken>());
    }

    private ConflictResolver CreateResolver()
        => new(_graphApiClient, _metadataRepo, _accountRepo, _conflictRepo, _localFileScanner, _logger);

    private static SyncConflict CreateTestConflict()
        => new(
            Id: "conflict-123",
            AccountId: "account-456",
            FilePath: "Documents/test.txt",
            LocalModifiedUtc: DateTime.UtcNow.AddHours(-1),
            RemoteModifiedUtc: DateTime.UtcNow,
            LocalSize: 100,
            RemoteSize: 200,
            DetectedUtc: DateTime.UtcNow,
            ResolutionStrategy: ConflictResolutionStrategy.None,
            IsResolved: false);

    private static AccountInfo CreateTestAccount()
        => new(
            AccountId: "account-456",
            DisplayName: "Test User",
            LocalSyncPath: Path.Combine(Path.GetTempPath(), Guid.CreateVersion7().ToString()),
            IsAuthenticated: true,
            LastSyncUtc: DateTime.UtcNow,
            DeltaToken: null,
            EnableDetailedSyncLogging: false,
            EnableDebugLogging: false,
            MaxParallelUpDownloads: 3,
            MaxItemsInBatch: 50,
            AutoSyncIntervalMinutes: null);

    private static FileMetadata CreateTestMetadata(string accountId, string filePath)
        => new(
            Id: "file-789",
            AccountId: accountId,
            Name: Path.GetFileName(filePath),
            Path: filePath,
            Size: 100,
            LastModifiedUtc: DateTime.UtcNow,
            LocalPath: filePath,
            CTag: "ctag-123",
            ETag: "etag-456",
            LocalHash: "hash-789",
            SyncStatus: FileSyncStatus.Synced,
            LastSyncDirection: SyncDirection.Download);
}
