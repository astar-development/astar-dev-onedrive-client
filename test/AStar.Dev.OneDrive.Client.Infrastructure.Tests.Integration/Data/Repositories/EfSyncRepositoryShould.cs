using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Integration.Data.Repositories;

public sealed class EfSyncRepositoryShould : IDisposable
{
    private readonly AppDbContext _context;
    private readonly EfSyncRepository _repository;

    private sealed class TestDbContextFactory(AppDbContext context) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => context;
    }

    public EfSyncRepositoryShould()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.OpenConnection();
        _ = _context.Database.EnsureCreated();

        var factory = new TestDbContextFactory(_context);
        _repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
    [Fact]
    public async Task GetAllPendingDownloadsAsync_ReturnsAllPendingDownloads()
    {
        DriveItemRecord[] items =
    [
        new DriveItemRecord("id1", "driveId1", "file1.txt", null, null, 100, DateTimeOffset.UtcNow.AddHours(-3), false, false),
        new DriveItemRecord("id2", "driveId2", "file2.txt", null, null, 200, DateTimeOffset.UtcNow.AddHours(-2), false, false),
        new DriveItemRecord("id3", "driveId3", "file3.txt", null, null, 300, DateTimeOffset.UtcNow.AddHours(-1), false, false)
    ];
        await _repository.ApplyDriveItemsAsync(items, CancellationToken.None);
        IEnumerable<DriveItemRecord> result = await _repository.GetAllPendingDownloadsAsync(CancellationToken.None);
        var resultList = result.ToList();
        resultList.Count.ShouldBe(3);
        resultList.ShouldContain(i => i.Id == "id1");
        resultList.ShouldContain(i => i.Id == "id2");
        resultList.ShouldContain(i => i.Id == "id3");
    }

    [Fact]
    public async Task GetAllPendingDownloadsAsync_ExcludesFoldersAndDeleted()
    {
        DriveItemRecord[] items =
    [
        new DriveItemRecord("id1", "driveId1", "file1.txt", null, null, 100, DateTimeOffset.UtcNow, false, false),
        new DriveItemRecord("id2", "driveId2", "folder", null, null, 0, DateTimeOffset.UtcNow, true, false),
        new DriveItemRecord("id3", "driveId3", "deleted.txt", null, null, 50, DateTimeOffset.UtcNow, false, true)
    ];
        await _repository.ApplyDriveItemsAsync(items, CancellationToken.None);
        IEnumerable<DriveItemRecord> result = await _repository.GetAllPendingDownloadsAsync(CancellationToken.None);
        var resultList = result.ToList();
        resultList.Count.ShouldBe(1);
        resultList[0].Id.ShouldBe("id1");
    }

    [Fact]
    public async Task GetAllPendingDownloadsAsync_ReturnsEmptyWhenNoPending()
    {
        IEnumerable<DriveItemRecord> result = await _repository.GetAllPendingDownloadsAsync(CancellationToken.None);
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllPendingDownloadsAsync_OrdersByLastModified()
    {
        DriveItemRecord[] items =
    [
        new DriveItemRecord("id1", "driveId1", "newest.txt", null, null, 100, DateTimeOffset.UtcNow, false, false),
        new DriveItemRecord("id2", "driveId2", "oldest.txt", null, null, 200, DateTimeOffset.UtcNow.AddHours(-5), false, false),
        new DriveItemRecord("id3", "driveId3", "middle.txt", null, null, 150, DateTimeOffset.UtcNow.AddHours(-2), false, false)
    ];
        await _repository.ApplyDriveItemsAsync(items, CancellationToken.None);
        IEnumerable<DriveItemRecord> result = await _repository.GetAllPendingDownloadsAsync(CancellationToken.None);
        var resultList = result.ToList();
        resultList[0].Id.ShouldBe("id2");
        resultList[1].Id.ShouldBe("id3");
        resultList[2].Id.ShouldBe("id1");
    }
    #region DeltaToken Tests

    [Fact]
    public async Task ReturnNullWhenNoDeltaTokenExists()
    {
        DeltaToken? result = await _repository.GetDeltaTokenAsync(CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task SaveNewDeltaToken()
    {
        DeltaToken token = new("token1", "deltaLink123", DateTimeOffset.UtcNow);

        await _repository.SaveOrUpdateDeltaTokenAsync(token, CancellationToken.None);

        DeltaToken? retrieved = await _repository.GetDeltaTokenAsync(CancellationToken.None);
        _ = retrieved.ShouldNotBeNull();
        retrieved.Id.ShouldBe("token1");
        retrieved.Token.ShouldBe("deltaLink123");
    }

    [Fact]
    public async Task UpdateExistingDeltaToken()
    {
        DeltaToken original = new("token1", "deltaLink123", DateTimeOffset.UtcNow.AddHours(-1));
        await _repository.SaveOrUpdateDeltaTokenAsync(original, CancellationToken.None);

        DeltaToken updated = new("token1", "deltaLink456", DateTimeOffset.UtcNow);
        await _repository.SaveOrUpdateDeltaTokenAsync(updated, CancellationToken.None);

        DeltaToken? retrieved = await _repository.GetDeltaTokenAsync(CancellationToken.None);
        _ = retrieved.ShouldNotBeNull();
        retrieved.Token.ShouldBe("deltaLink456");
    }

    [Fact]
    public async Task GetLatestDeltaTokenWhenMultipleExist()
    {
        DeltaToken older = new("token1", "old", DateTimeOffset.UtcNow.AddHours(-2));
        DeltaToken newer = new("token2", "new", DateTimeOffset.UtcNow);

        await _repository.SaveOrUpdateDeltaTokenAsync(older, CancellationToken.None);
        await _repository.SaveOrUpdateDeltaTokenAsync(newer, CancellationToken.None);

        DeltaToken? result = await _repository.GetDeltaTokenAsync(CancellationToken.None);

        _ = result.ShouldNotBeNull();
        result.Id.ShouldBe("token2");
        result.Token.ShouldBe("new");
    }

    #endregion

    #region DriveItem Tests

    [Fact]
    public async Task ApplyNewDriveItems()
    {
        DriveItemRecord[] items =
        [
            new DriveItemRecord("id1", "driveId1", "file1.txt", "etag1", "ctag1", 100, DateTimeOffset.UtcNow, false, false),
            new DriveItemRecord("id2", "driveId2", "file2.txt", "etag2", "ctag2", 200, DateTimeOffset.UtcNow, false, false)
        ];

        await _repository.ApplyDriveItemsAsync(items, CancellationToken.None);

        List<DriveItemRecord> allItems = await _context.DriveItems.ToListAsync(TestContext.Current.CancellationToken);
        allItems.Count.ShouldBe(2);
        allItems.ShouldContain(i => i.Id == "id1");
        allItems.ShouldContain(i => i.Id == "id2");
    }

    [Fact]
    public async Task UpdateExistingDriveItems()
    {
        DriveItemRecord original = new("id1", "driveId1", "file1.txt", "etag1", "ctag1", 100, DateTimeOffset.UtcNow.AddHours(-1), false, false);
        await _repository.ApplyDriveItemsAsync([original], CancellationToken.None);

        DriveItemRecord updated = new("id1", "driveId1", "file1.txt", "etag2", "ctag2", 150, DateTimeOffset.UtcNow, false, false);
        await _repository.ApplyDriveItemsAsync([updated], CancellationToken.None);

        DriveItemRecord? result = await _context.DriveItems.FindAsync(["id1"], TestContext.Current.CancellationToken);
        _ = result.ShouldNotBeNull();
        result.ETag.ShouldBe("etag2");
        result.Size.ShouldBe(150);

        var count = await _context.DriveItems.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task ApplyMixedNewAndExistingDriveItems()
    {
        DriveItemRecord existing = new("id1", "driveId1", "file1.txt", "etag1", "ctag1", 100, DateTimeOffset.UtcNow.AddHours(-1), false, false);
        await _repository.ApplyDriveItemsAsync([existing], CancellationToken.None);

        DriveItemRecord[] mixed =
        [
            new DriveItemRecord("id1", "driveId1", "file1.txt", "etag2", "ctag2", 150, DateTimeOffset.UtcNow, false, false),
            new DriveItemRecord("id2", "driveId2", "file2.txt", "etag3", "ctag3", 200, DateTimeOffset.UtcNow, false, false)
        ];
        await _repository.ApplyDriveItemsAsync(mixed, CancellationToken.None);

        List<DriveItemRecord> allItems = await _context.DriveItems.ToListAsync(TestContext.Current.CancellationToken);
        allItems.Count.ShouldBe(2);
        allItems.First(i => i.Id == "id1").ETag.ShouldBe("etag2");
    }

    [Fact]
    public async Task GetPendingDownloadsExcludesFoldersAndDeleted()
    {
        DriveItemRecord[] items =
        [
            new DriveItemRecord("id1", "driveId1", "file1.txt", null, null, 100, DateTimeOffset.UtcNow.AddHours(-3), false, false),
            new DriveItemRecord("id2", "driveId2", "folder", null, null, 0, DateTimeOffset.UtcNow.AddHours(-2), true, false),
            new DriveItemRecord("id3", "driveId3", "deleted.txt", null, null, 50, DateTimeOffset.UtcNow.AddHours(-1), false, true),
            new DriveItemRecord("id4", "driveId4", "file2.txt", null, null, 200, DateTimeOffset.UtcNow, false, false)
        ];
        await _repository.ApplyDriveItemsAsync(items, CancellationToken.None);

        IEnumerable<DriveItemRecord> result = await _repository.GetPendingDownloadsAsync(10, 0, CancellationToken.None);

        var resultList = result.ToList();
        resultList.Count.ShouldBe(2);
        resultList.ShouldNotContain(i => i.IsFolder);
        resultList.ShouldNotContain(i => i.IsDeleted);
        resultList.ShouldContain(i => i.Id == "id1");
        resultList.ShouldContain(i => i.Id == "id4");
    }

    [Fact]
    public async Task GetPendingDownloadsOrdersByLastModified()
    {
        DriveItemRecord[] items =
        [
            new DriveItemRecord("id1", "driveId1", "newest.txt", null, null, 100, DateTimeOffset.UtcNow, false, false),
            new DriveItemRecord("id2", "driveId2", "oldest.txt", null, null, 200, DateTimeOffset.UtcNow.AddHours(-5), false, false),
            new DriveItemRecord("id3", "driveId3", "middle.txt", null, null, 150, DateTimeOffset.UtcNow.AddHours(-2), false, false)
        ];
        await _repository.ApplyDriveItemsAsync(items, CancellationToken.None);

        IEnumerable<DriveItemRecord> result = await _repository.GetPendingDownloadsAsync(10, 0, CancellationToken.None);

        var resultList = result.ToList();
        resultList[0].Id.ShouldBe("id2");
        resultList[1].Id.ShouldBe("id3");
        resultList[2].Id.ShouldBe("id1"); // newest last
    }

    [Fact]
    public async Task GetPendingDownloadsSupportsPagination()
    {
        DriveItemRecord[] items = [.. Enumerable.Range(1, 10).Select(i => new DriveItemRecord($"id{i}", $"driveId{i}", $"file{i}.txt", null, null, i * 100, DateTimeOffset.UtcNow.AddHours(-i), false, false))];
        await _repository.ApplyDriveItemsAsync(items, CancellationToken.None);

        IEnumerable<DriveItemRecord> page1 = await _repository.GetPendingDownloadsAsync(3, 0, CancellationToken.None);
        IEnumerable<DriveItemRecord> page2 = await _repository.GetPendingDownloadsAsync(3, 1, CancellationToken.None);

        page1.Count().ShouldBe(3);
        page2.Count().ShouldBe(3);
        page1.First().Id.ShouldBe("id10"); // oldest
        page2.First().Id.ShouldBe("id7");
    }

    [Fact]
    public async Task GetPendingDownloadCountReturnsCorrectCount()
    {
        DriveItemRecord[] items =
        [
            new DriveItemRecord("id1", "driveId1", "file1.txt", null, null, 100, DateTimeOffset.UtcNow, false, false),
            new DriveItemRecord("id2", "driveId2", "folder", null, null, 0, DateTimeOffset.UtcNow, true, false),
            new DriveItemRecord("id3", "driveId3", "file2.txt", null, null, 200, DateTimeOffset.UtcNow, false, false)
        ];
        await _repository.ApplyDriveItemsAsync(items, CancellationToken.None);

        var count = await _repository.GetPendingDownloadCountAsync(CancellationToken.None);

        count.ShouldBe(2);
    }

    [Fact]
    public async Task GetPendingDownloadCountReturnsZeroWhenNoPendingDownloads()
    {
        var count = await _repository.GetPendingDownloadCountAsync(CancellationToken.None);

        count.ShouldBe(0);
    }

    #endregion

    #region LocalFile Tests

    [Fact]
    public async Task AddNewLocalFileRecord()
    {
        LocalFileRecord file = new("id1", "file1.txt", "hash123", 100, DateTimeOffset.UtcNow, SyncState.Downloaded);

        await _repository.AddOrUpdateLocalFileAsync(file, CancellationToken.None);

        LocalFileRecord? result = await _context.LocalFiles.FindAsync(["id1"], TestContext.Current.CancellationToken);
        _ = result.ShouldNotBeNull();
        result.RelativePath.ShouldBe("file1.txt");
        result.Hash.ShouldBe("hash123");
        result.SyncState.ShouldBe(SyncState.Downloaded);
    }

    [Fact]
    public async Task UpdateExistingLocalFileRecord()
    {
        LocalFileRecord original = new("id1", "file1.txt", "hash123", 100, DateTimeOffset.UtcNow.AddHours(-1), SyncState.PendingDownload);
        await _repository.AddOrUpdateLocalFileAsync(original, CancellationToken.None);

        LocalFileRecord updated = new("id1", "file1.txt", "hash456", 150, DateTimeOffset.UtcNow, SyncState.Downloaded);
        await _repository.AddOrUpdateLocalFileAsync(updated, CancellationToken.None);

        LocalFileRecord? result = await _context.LocalFiles.FindAsync(["id1"], TestContext.Current.CancellationToken);
        _ = result.ShouldNotBeNull();
        result.Hash.ShouldBe("hash456");
        result.Size.ShouldBe(150);
        result.SyncState.ShouldBe(SyncState.Downloaded);

        var count = await _context.LocalFiles.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task MarkLocalFileStateCreatesNewRecordWhenDriveItemExists()
    {
        DriveItemRecord driveItem = new("id1", "driveId1", "file1.txt", null, null, 100, DateTimeOffset.UtcNow, false, false);
        await _repository.ApplyDriveItemsAsync([driveItem], CancellationToken.None);

        await _repository.MarkLocalFileStateAsync("id1", SyncState.Downloaded, CancellationToken.None);

        LocalFileRecord? result = await _context.LocalFiles.FindAsync(["id1"], TestContext.Current.CancellationToken);
        _ = result.ShouldNotBeNull();
        result.RelativePath.ShouldBe("file1.txt");
        result.SyncState.ShouldBe(SyncState.Downloaded);
        result.Size.ShouldBe(100);
    }

    [Fact]
    public async Task MarkLocalFileStateUpdatesExistingRecord()
    {
        DriveItemRecord driveItem = new("id1", "driveId1", "file1.txt", null, null, 150, DateTimeOffset.UtcNow, false, false);
        await _repository.ApplyDriveItemsAsync([driveItem], CancellationToken.None);

        LocalFileRecord localFile = new("id1", "file1.txt", "hash123", 100, DateTimeOffset.UtcNow.AddHours(-1), SyncState.PendingDownload);
        await _repository.AddOrUpdateLocalFileAsync(localFile, CancellationToken.None);

        await _repository.MarkLocalFileStateAsync("id1", SyncState.Downloaded, CancellationToken.None);

        LocalFileRecord? result = await _context.LocalFiles.FindAsync(["id1"], TestContext.Current.CancellationToken);
        _ = result.ShouldNotBeNull();
        result.SyncState.ShouldBe(SyncState.Downloaded);
        result.Size.ShouldBe(150);
    }

    [Fact]
    public async Task MarkLocalFileStateDoesNothingWhenDriveItemNotFound()
    {
        await _repository.MarkLocalFileStateAsync("nonexistent", SyncState.Downloaded, CancellationToken.None);

        var count = await _context.LocalFiles.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(0);
    }

    [Fact]
    public async Task GetPendingUploadsReturnsOnlyPendingUploadFiles()
    {
        LocalFileRecord[] files =
        [
            new LocalFileRecord("id1", "file1.txt", null, 100, DateTimeOffset.UtcNow, SyncState.PendingUpload),
            new LocalFileRecord("id2", "file2.txt", null, 200, DateTimeOffset.UtcNow, SyncState.Downloaded),
            new LocalFileRecord("id3", "file3.txt", null, 150, DateTimeOffset.UtcNow, SyncState.PendingUpload),
            new LocalFileRecord("id4", "file4.txt", null, 250, DateTimeOffset.UtcNow, SyncState.Uploaded)
        ];

        foreach(LocalFileRecord file in files)
        {
            await _repository.AddOrUpdateLocalFileAsync(file, CancellationToken.None);
        }

        IEnumerable<LocalFileRecord> result = await _repository.GetPendingUploadsAsync(10, CancellationToken.None);

        var resultList = result.ToList();
        resultList.Count.ShouldBe(2);
        resultList.ShouldAllBe(f => f.SyncState == SyncState.PendingUpload);
    }

    [Fact]
    public async Task GetPendingUploadsRespectsLimit()
    {
        LocalFileRecord[] files = [.. Enumerable.Range(1, 5).Select(i => new LocalFileRecord($"id{i}", $"file{i}.txt", null, i * 100, DateTimeOffset.UtcNow, SyncState.PendingUpload))];

        foreach(LocalFileRecord file in files)
        {
            await _repository.AddOrUpdateLocalFileAsync(file, CancellationToken.None);
        }

        IEnumerable<LocalFileRecord> result = await _repository.GetPendingUploadsAsync(3, CancellationToken.None);

        result.Count().ShouldBe(3);
    }

    #endregion

    #region TransferLog Tests

    [Fact]
    public async Task LogNewTransfer()
    {
        TransferLog log = new("log1", TransferType.Download, "item1", DateTimeOffset.UtcNow, null, TransferStatus.Pending, null, null);

        await _repository.LogTransferAsync(log, CancellationToken.None);

        TransferLog? result = await _context.TransferLogs.FindAsync(["log1"], TestContext.Current.CancellationToken);
        _ = result.ShouldNotBeNull();
        result.Type.ShouldBe(TransferType.Download);
        result.Status.ShouldBe(TransferStatus.Pending);
    }

    [Fact]
    public async Task UpdateExistingTransferLog()
    {
        TransferLog original = new("log1", TransferType.Download, "item1", DateTimeOffset.UtcNow.AddMinutes(-5), null, TransferStatus.InProgress, 1024, null);
        await _repository.LogTransferAsync(original, CancellationToken.None);

        TransferLog updated = new("log1", TransferType.Download, "item1", DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow, TransferStatus.Success, 2048, null);
        await _repository.LogTransferAsync(updated, CancellationToken.None);

        TransferLog? result = await _context.TransferLogs.FindAsync(["log1"], TestContext.Current.CancellationToken);
        _ = result.ShouldNotBeNull();
        result.Status.ShouldBe(TransferStatus.Success);
        _ = result.CompletedUtc.ShouldNotBeNull();
        result.BytesTransferred.ShouldBe(2048);

        var count = await _context.TransferLogs.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task LogTransferWithNullOptionalFields()
    {
        TransferLog log = new("log1", TransferType.Upload, "item1", DateTimeOffset.UtcNow, null, TransferStatus.Pending, null, null);

        await _repository.LogTransferAsync(log, CancellationToken.None);

        TransferLog? result = await _context.TransferLogs.FindAsync(["log1"], TestContext.Current.CancellationToken);
        _ = result.ShouldNotBeNull();
        result.CompletedUtc.ShouldBeNull();
        result.BytesTransferred.ShouldBeNull();
        result.Error.ShouldBeNull();
    }

    [Fact]
    public async Task LogTransferWithAllFieldsPopulated()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        TransferLog log = new("log1", TransferType.Download, "item1", now.AddMinutes(-10), now, TransferStatus.Failed, 512, "Network timeout");

        await _repository.LogTransferAsync(log, CancellationToken.None);

        TransferLog? result = await _context.TransferLogs.FindAsync(["log1"], TestContext.Current.CancellationToken);
        _ = result.ShouldNotBeNull();
        _ = result.CompletedUtc.ShouldNotBeNull();
        result.BytesTransferred.ShouldBe(512);
        result.Error.ShouldBe("Network timeout");
    }

    [Fact]
    public async Task LogMultipleTransferTypes()
    {
        TransferLog[] logs =
        [
            new TransferLog("log1", TransferType.Download, "item1", DateTimeOffset.UtcNow, null, TransferStatus.Pending, null, null),
            new TransferLog("log2", TransferType.Upload, "item2", DateTimeOffset.UtcNow, null, TransferStatus.InProgress, null, null),
            new TransferLog("log3", TransferType.Delete, "item3", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, TransferStatus.Success, null, null)
        ];

        foreach(TransferLog log in logs)
        {
            await _repository.LogTransferAsync(log, CancellationToken.None);
        }

        List<TransferLog> allLogs = await _context.TransferLogs.ToListAsync(TestContext.Current.CancellationToken);
        allLogs.Count.ShouldBe(3);
        allLogs.ShouldContain(l => l.Type == TransferType.Download);
        allLogs.ShouldContain(l => l.Type == TransferType.Upload);
        allLogs.ShouldContain(l => l.Type == TransferType.Delete);
    }

    #endregion
}
