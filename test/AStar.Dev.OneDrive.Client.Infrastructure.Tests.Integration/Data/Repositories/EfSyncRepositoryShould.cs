using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Integration.Data.Repositories;

public sealed class EfSyncRepositoryShould : IAsyncLifetime
{
    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        public TestDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;
        public AppDbContext CreateDbContext() => new(_options);
    }
    // Remove shared context and repository fields
    private string _connectionString = null!;
    private SqliteConnection _connection = null!;

    public async ValueTask InitializeAsync()
    {
        // Use a unique in-memory database for each test class instance
        _connectionString = $"DataSource=file:memdb-{Guid.NewGuid()};Mode=Memory;Cache=Shared";
        _connection = new SqliteConnection(_connectionString);
        await _connection.OpenAsync();

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Ensure database is created for the test class
        using var context = new AppDbContext(options);
        _ = await context.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();

    [Fact]
    public async Task GetAllPendingDownloadsAsync_ReturnsAllPendingDownloads()
    {
        DriveItemRecord[] items =
    [
        new DriveItemRecord("PlaceholderAccountId", "id1", "driveId1", "file1.txt", null, null, 100, DateTimeOffset.UtcNow.AddHours(-3), false, false),
        new DriveItemRecord("PlaceholderAccountId", "id2", "driveId2", "file2.txt", null, null, 200, DateTimeOffset.UtcNow.AddHours(-2), false, false),
        new DriveItemRecord("PlaceholderAccountId", "id3", "driveId3", "file3.txt", null, null, 300, DateTimeOffset.UtcNow.AddHours(-1), false, false)
    ];
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        await repository.ApplyDriveItemsAsync("PlaceholderAccountId", items, CancellationToken.None);
        IEnumerable<DriveItemRecord> result = await repository.GetAllPendingDownloadsAsync("PlaceholderAccountId", CancellationToken.None);
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
        new DriveItemRecord("PlaceholderAccountId", "id1", "driveId1", "file1.txt", null, null, 100, DateTimeOffset.UtcNow, false, false),
        new DriveItemRecord("PlaceholderAccountId", "id2", "driveId2", "folder", null, null, 0, DateTimeOffset.UtcNow, true, false),
        new DriveItemRecord("PlaceholderAccountId", "id3", "driveId3", "deleted.txt", null, null, 50, DateTimeOffset.UtcNow, false, true)
    ];
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        await repository.ApplyDriveItemsAsync("PlaceholderAccountId", items, CancellationToken.None);
        IEnumerable<DriveItemRecord> result = await repository.GetAllPendingDownloadsAsync("PlaceholderAccountId", CancellationToken.None);
        var resultList = result.ToList();
        resultList.Count.ShouldBe(1);
        resultList[0].Id.ShouldBe("id1");
    }

    [Fact]
    public async Task GetAllPendingDownloadsAsync_ReturnsEmptyWhenNoPending()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        IEnumerable<DriveItemRecord> result = await repository.GetAllPendingDownloadsAsync("PlaceholderAccountId", CancellationToken.None);
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllPendingDownloadsAsync_OrdersByLastModified()
    {
        DriveItemRecord[] items =
    [
        new DriveItemRecord("PlaceholderAccountId", "id1", "driveId1", "newest.txt", null, null, 100, DateTimeOffset.UtcNow, false, false),
        new DriveItemRecord("PlaceholderAccountId", "id2", "driveId2", "oldest.txt", null, null, 200, DateTimeOffset.UtcNow.AddHours(-5), false, false),
        new DriveItemRecord("PlaceholderAccountId", "id3", "driveId3", "middle.txt", null, null, 150, DateTimeOffset.UtcNow.AddHours(-2), false, false)
    ];
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        await repository.ApplyDriveItemsAsync("PlaceholderAccountId", items, CancellationToken.None);
        IEnumerable<DriveItemRecord> result = await repository.GetAllPendingDownloadsAsync("PlaceholderAccountId", CancellationToken.None);
        var resultList = result.ToList();
        resultList[0].Id.ShouldBe("id2");
        resultList[1].Id.ShouldBe("id3");
        resultList[2].Id.ShouldBe("id1");
    }
    #region DeltaToken Tests

    [Fact]
    public async Task ReturnNullWhenNoDeltaTokenExists()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        DeltaToken? result = await repository.GetDeltaTokenAsync("PlaceholderAccountId", CancellationToken.None);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SaveNewDeltaToken()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        var token = new DeltaToken("PlaceholderAccountId", "token1", "deltaLink123", DateTimeOffset.UtcNow);
        await repository.SaveOrUpdateDeltaTokenAsync("PlaceholderAccountId", token, CancellationToken.None);
        DeltaToken? retrieved = await repository.GetDeltaTokenAsync("PlaceholderAccountId", CancellationToken.None);
        _ = retrieved.ShouldNotBeNull();
        retrieved.Id.ShouldBe("token1");
        retrieved.Token.ShouldBe("deltaLink123");
    }

    [Fact]
    public async Task UpdateExistingDeltaToken()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        var original = new DeltaToken("PlaceholderAccountId", "token1", "deltaLink123", DateTimeOffset.UtcNow.AddHours(-1));
        await repository.SaveOrUpdateDeltaTokenAsync("PlaceholderAccountId", original, CancellationToken.None);
        var updated = new DeltaToken("PlaceholderAccountId", "token1", "deltaLink456", DateTimeOffset.UtcNow);
        await repository.SaveOrUpdateDeltaTokenAsync("PlaceholderAccountId", updated, CancellationToken.None);
        DeltaToken? retrieved = await repository.GetDeltaTokenAsync("PlaceholderAccountId", CancellationToken.None);
        _ = retrieved.ShouldNotBeNull();
        retrieved.Token.ShouldBe("deltaLink456");
    }

    [Fact]
    public async Task GetLatestDeltaTokenWhenMultipleExist()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        var older = new DeltaToken("PlaceholderAccountId", "token1", "old", DateTimeOffset.UtcNow.AddHours(-2));
        var newer = new DeltaToken("PlaceholderAccountId", "token2", "new", DateTimeOffset.UtcNow);
        await repository.SaveOrUpdateDeltaTokenAsync("PlaceholderAccountId", older, CancellationToken.None);
        await repository.SaveOrUpdateDeltaTokenAsync("PlaceholderAccountId", newer, CancellationToken.None);
        DeltaToken? result = await repository.GetDeltaTokenAsync("PlaceholderAccountId", CancellationToken.None);
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
            new DriveItemRecord("PlaceholderAccountId", "id1", "driveId1", "file1.txt", "etag1", "ctag1", 100, DateTimeOffset.UtcNow, false, false),
            new DriveItemRecord("PlaceholderAccountId", "id2", "driveId2", "file2.txt", "etag2", "ctag2", 200, DateTimeOffset.UtcNow, false, false)
        ];

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        await repository.ApplyDriveItemsAsync("PlaceholderAccountId", items, CancellationToken.None);
        using var assertionContext = new AppDbContext(options);
        List<DriveItemRecord> allItems = await assertionContext.DriveItems.ToListAsync(TestContext.Current.CancellationToken);
        allItems.Count.ShouldBe(2);
        allItems.ShouldContain(i => i.Id == "id1");
        allItems.ShouldContain(i => i.Id == "id2");
    }

    [Fact]
    public async Task UpdateExistingDriveItems()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        var original = new DriveItemRecord("PlaceholderAccountId", "id1", "driveId1", "file1.txt", "etag1", "ctag1", 100, DateTimeOffset.UtcNow.AddHours(-1), false, false);
        await repository.ApplyDriveItemsAsync("PlaceholderAccountId", new[] { original }, CancellationToken.None);
        var updated = new DriveItemRecord("PlaceholderAccountId", "id1", "driveId1", "file1.txt", "etag2", "ctag2", 150, DateTimeOffset.UtcNow, false, false);
        await repository.ApplyDriveItemsAsync("PlaceholderAccountId", new[] { updated }, CancellationToken.None);
        using var assertionContext = new AppDbContext(options);
        DriveItemRecord? result = await assertionContext.DriveItems.FindAsync(["id1"], TestContext.Current.CancellationToken);
        _ = result.ShouldNotBeNull();
        result.ETag.ShouldBe("etag2");
        result.Size.ShouldBe(150);
        var count = await assertionContext.DriveItems.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task ApplyMixedNewAndExistingDriveItems()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        var existing = new DriveItemRecord("PlaceholderAccountId", "id1", "driveId1", "file1.txt", "etag1", "ctag1", 100, DateTimeOffset.UtcNow.AddHours(-1), false, false);
        await repository.ApplyDriveItemsAsync("PlaceholderAccountId", new[] { existing }, CancellationToken.None);
        DriveItemRecord[] mixed =
        [
            new DriveItemRecord("PlaceholderAccountId", "id1", "driveId1", "file1.txt", "etag2", "ctag2", 150, DateTimeOffset.UtcNow, false, false),
            new DriveItemRecord("PlaceholderAccountId", "id2", "driveId2", "file2.txt", "etag3", "ctag3", 200, DateTimeOffset.UtcNow, false, false)
        ];
        await repository.ApplyDriveItemsAsync("PlaceholderAccountId", mixed, CancellationToken.None);
        using var assertionContext = new AppDbContext(options);
        List<DriveItemRecord> allItems = await assertionContext.DriveItems.ToListAsync(TestContext.Current.CancellationToken);
        allItems.Count.ShouldBe(2);
        allItems.First(i => i.Id == "id1").ETag.ShouldBe("etag2");
    }

    [Fact]
    public async Task GetPendingDownloadsExcludesFoldersAndDeleted()
    {
        DriveItemRecord[] items =
        [
            new DriveItemRecord("PlaceholderAccountId", "id1", "driveId1", "file1.txt", null, null, 100, DateTimeOffset.UtcNow.AddHours(-3), false, false),
            new DriveItemRecord("PlaceholderAccountId", "id2", "driveId2", "folder", null, null, 0, DateTimeOffset.UtcNow.AddHours(-2), true, false),
            new DriveItemRecord("PlaceholderAccountId", "id3", "driveId3", "deleted.txt", null, null, 50, DateTimeOffset.UtcNow.AddHours(-1), false, true),
            new DriveItemRecord("PlaceholderAccountId", "id4", "driveId4", "file2.txt", null, null, 200, DateTimeOffset.UtcNow, false, false)
        ];
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        await repository.ApplyDriveItemsAsync("PlaceholderAccountId", items, CancellationToken.None);
        IEnumerable<DriveItemRecord> result = await repository.GetPendingDownloadsAsync("PlaceholderAccountId", 10, 0, CancellationToken.None);
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
            new DriveItemRecord("PlaceholderAccountId", "id1", "driveId1", "newest.txt", null, null, 100, DateTimeOffset.UtcNow, false, false),
            new DriveItemRecord("PlaceholderAccountId", "id2", "driveId2", "oldest.txt", null, null, 200, DateTimeOffset.UtcNow.AddHours(-5), false, false),
            new DriveItemRecord("PlaceholderAccountId", "id3", "driveId3", "middle.txt", null, null, 150, DateTimeOffset.UtcNow.AddHours(-2), false, false)
        ];
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        await repository.ApplyDriveItemsAsync("PlaceholderAccountId", items, CancellationToken.None);
        IEnumerable<DriveItemRecord> result = await repository.GetPendingDownloadsAsync("PlaceholderAccountId", 10, 0, CancellationToken.None);
        var resultList = result.ToList();
        resultList[0].Id.ShouldBe("id2");
        resultList[1].Id.ShouldBe("id3");
        resultList[2].Id.ShouldBe("id1"); // newest last
    }

    [Fact]
    public async Task GetPendingDownloadsSupportsPagination()
    {
        DriveItemRecord[] items = [.. Enumerable.Range(1, 10).Select(i => new DriveItemRecord("PlaceholderAccountId", $"id{i}", $"driveId{i}", $"file{i}.txt", null, null, i * 100, DateTimeOffset.UtcNow.AddHours(-i), false, false))];
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        await repository.ApplyDriveItemsAsync("PlaceholderAccountId", items, CancellationToken.None);
        IEnumerable<DriveItemRecord> page1 = await repository.GetPendingDownloadsAsync("PlaceholderAccountId", 3, 0, CancellationToken.None);
        IEnumerable<DriveItemRecord> page2 = await repository.GetPendingDownloadsAsync("PlaceholderAccountId", 3, 1, CancellationToken.None);
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
            new DriveItemRecord("PlaceholderAccountId", "id1", "driveId1", "file1.txt", null, null, 100, DateTimeOffset.UtcNow, false, false),
            new DriveItemRecord("PlaceholderAccountId", "id2", "driveId2", "folder", null, null, 0, DateTimeOffset.UtcNow, true, false),
            new DriveItemRecord("PlaceholderAccountId", "id3", "driveId3", "file2.txt", null, null, 200, DateTimeOffset.UtcNow, false, false)
        ];
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        await repository.ApplyDriveItemsAsync("PlaceholderAccountId", items, CancellationToken.None);
        var count = await repository.GetPendingDownloadCountAsync("PlaceholderAccountId", CancellationToken.None);
        count.ShouldBe(2);
    }

    [Fact]
    public async Task GetPendingDownloadCountReturnsZeroWhenNoPendingDownloads()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        var count = await repository.GetPendingDownloadCountAsync("PlaceholderAccountId", CancellationToken.None);
        count.ShouldBe(0);
    }

    #endregion

    #region LocalFile Tests

    [Fact]
    public async Task AddNewLocalFileRecord()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        var file = new LocalFileRecord("PlaceholderAccountId", "id1", "file1.txt", "hash123", 100, DateTimeOffset.UtcNow, SyncState.Downloaded);
        await repository.AddOrUpdateLocalFileAsync("PlaceholderAccountId", file, CancellationToken.None);
        using var assertionContext = new AppDbContext(options);
        LocalFileRecord? result = await assertionContext.LocalFiles.FindAsync(["id1"], TestContext.Current.CancellationToken);
        _ = result.ShouldNotBeNull();
        result.RelativePath.ShouldBe("file1.txt");
        result.Hash.ShouldBe("hash123");
        result.SyncState.ShouldBe(SyncState.Downloaded);
    }

    [Fact]
    public async Task UpdateExistingLocalFileRecord()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        var original = new LocalFileRecord("PlaceholderAccountId", "id1", "file1.txt", "hash123", 100, DateTimeOffset.UtcNow.AddHours(-1), SyncState.PendingDownload);
        await repository.AddOrUpdateLocalFileAsync("PlaceholderAccountId", original, CancellationToken.None);
    var updated = new LocalFileRecord("PlaceholderAccountId", "id1", "file1.txt", "hash456", 150, DateTimeOffset.UtcNow, SyncState.Downloaded);
        await repository.AddOrUpdateLocalFileAsync("PlaceholderAccountId", updated, CancellationToken.None);
        using var assertionContext = new AppDbContext(options);
        LocalFileRecord? result = await assertionContext.LocalFiles.FindAsync(["id1"], TestContext.Current.CancellationToken);
        _ = result.ShouldNotBeNull();
        result.Hash.ShouldBe("hash456");
        result.Size.ShouldBe(150);
        result.SyncState.ShouldBe(SyncState.Downloaded);
        var count = await assertionContext.LocalFiles.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task MarkLocalFileStateCreatesNewRecordWhenDriveItemExists()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        var driveItem = new DriveItemRecord("PlaceholderAccountId", "id1", "driveId1", "file1.txt", null, null, 100, DateTimeOffset.UtcNow, false, false);
        await repository.ApplyDriveItemsAsync("PlaceholderAccountId", new[] { driveItem }, CancellationToken.None);
        await repository.MarkLocalFileStateAsync("PlaceholderAccountId", "id1", SyncState.Downloaded, CancellationToken.None);
        using var assertionContext = new AppDbContext(options);
        LocalFileRecord? result = await assertionContext.LocalFiles.FindAsync(["id1"], TestContext.Current.CancellationToken);
        _ = result.ShouldNotBeNull();
        result.RelativePath.ShouldBe("file1.txt");
        result.SyncState.ShouldBe(SyncState.Downloaded);
        result.Size.ShouldBe(100);
    }

    [Fact]
    public async Task MarkLocalFileStateUpdatesExistingRecord()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        var driveItem = new DriveItemRecord("PlaceholderAccountId", "id1", "driveId1", "file1.txt", null, null, 150, DateTimeOffset.UtcNow, false, false);
        await repository.ApplyDriveItemsAsync("PlaceholderAccountId", new[] { driveItem }, CancellationToken.None);
        var localFile = new LocalFileRecord("PlaceholderAccountId", "id1", "file1.txt", "hash123", 100, DateTimeOffset.UtcNow.AddHours(-1), SyncState.PendingDownload);
        await repository.AddOrUpdateLocalFileAsync("PlaceholderAccountId", localFile, CancellationToken.None);
        await repository.MarkLocalFileStateAsync("PlaceholderAccountId", "id1", SyncState.Downloaded, CancellationToken.None);
        using var assertionContext = new AppDbContext(options);
        LocalFileRecord? result = await assertionContext.LocalFiles.FindAsync(["id1"], TestContext.Current.CancellationToken);
        _ = result.ShouldNotBeNull();
        result.SyncState.ShouldBe(SyncState.Downloaded);
        result.Size.ShouldBe(150);
    }

    [Fact]
    public async Task MarkLocalFileStateDoesNothingWhenDriveItemNotFound()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        await repository.MarkLocalFileStateAsync("PlaceholderAccountId", "nonexistent", SyncState.Downloaded, CancellationToken.None);
        using var assertionContext = new AppDbContext(options);
        var count = await assertionContext.LocalFiles.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(0);
    }

    [Fact]
    public async Task GetPendingUploadsReturnsOnlyPendingUploadFiles()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        LocalFileRecord[] files =
        [
            new LocalFileRecord("PlaceholderAccountId", "id1", "file1.txt", null, 100, DateTimeOffset.UtcNow, SyncState.PendingUpload),
            new LocalFileRecord("PlaceholderAccountId", "id2", "file2.txt", null, 200, DateTimeOffset.UtcNow, SyncState.Downloaded),
            new LocalFileRecord("PlaceholderAccountId", "id3", "file3.txt", null, 150, DateTimeOffset.UtcNow, SyncState.PendingUpload),
            new LocalFileRecord("PlaceholderAccountId", "id4", "file4.txt", null, 250, DateTimeOffset.UtcNow, SyncState.Uploaded)
        ];
        foreach(LocalFileRecord? file in files)
        {
            await repository.AddOrUpdateLocalFileAsync("PlaceholderAccountId", file, CancellationToken.None);
        }

        IEnumerable<LocalFileRecord> result = await repository.GetPendingUploadsAsync("PlaceholderAccountId", 10, CancellationToken.None);
        var resultList = result.ToList();
        resultList.Count.ShouldBe(2);
        resultList.ShouldAllBe(f => f.SyncState == SyncState.PendingUpload);
    }

    [Fact]
    public async Task GetPendingUploadsRespectsLimit()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        LocalFileRecord[] files = Enumerable.Range(1, 5).Select(i => new LocalFileRecord("PlaceholderAccountId", $"id{i}", $"file{i}.txt", null, i * 100, DateTimeOffset.UtcNow, SyncState.PendingUpload)).ToArray();
        foreach(LocalFileRecord? file in files)
        {
            await repository.AddOrUpdateLocalFileAsync("PlaceholderAccountId", file, CancellationToken.None);
        }

        IEnumerable<LocalFileRecord> result = await repository.GetPendingUploadsAsync("PlaceholderAccountId", 3, CancellationToken.None);
        result.Count().ShouldBe(3);
    }

    #endregion

    #region TransferLog Tests

    [Fact]
    public async Task LogNewTransfer()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        var log = new TransferLog("PlaceholderAccountId", "log1", TransferType.Download, "item1", DateTimeOffset.UtcNow, null, TransferStatus.Pending, null, null);
        await repository.LogTransferAsync("PlaceholderAccountId", log, CancellationToken.None);
        using var assertionContext = new AppDbContext(options);
        TransferLog? result = await assertionContext.TransferLogs.FindAsync(["log1"], TestContext.Current.CancellationToken);
        _ = result.ShouldNotBeNull();
        result.Type.ShouldBe(TransferType.Download);
        result.Status.ShouldBe(TransferStatus.Pending);
    }

    [Fact]
    public async Task UpdateExistingTransferLog()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        var original = new TransferLog("PlaceholderAccountId", "log1", TransferType.Download, "item1", DateTimeOffset.UtcNow.AddMinutes(-5), null, TransferStatus.InProgress, 1024, null);
        await repository.LogTransferAsync("PlaceholderAccountId", original, CancellationToken.None);
        var updated = new TransferLog("PlaceholderAccountId", "log1", TransferType.Download, "item1", DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow, TransferStatus.Success, 2048, null);
        await repository.LogTransferAsync("PlaceholderAccountId", updated, CancellationToken.None);
        using var assertionContext = new AppDbContext(options);
        TransferLog? result = await assertionContext.TransferLogs.FindAsync(["log1"], TestContext.Current.CancellationToken);
        _ = result.ShouldNotBeNull();
        result.Status.ShouldBe(TransferStatus.Success);
        _ = result.CompletedUtc.ShouldNotBeNull();
        result.BytesTransferred.ShouldBe(2048);
        var count = await assertionContext.TransferLogs.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task LogTransferWithNullOptionalFields()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        var log = new TransferLog("PlaceholderAccountId", "log1", TransferType.Upload, "item1", DateTimeOffset.UtcNow, null, TransferStatus.Pending, null, null);
        await repository.LogTransferAsync("PlaceholderAccountId", log, CancellationToken.None);
        using var assertionContext = new AppDbContext(options);
        TransferLog? result = await assertionContext.TransferLogs.FindAsync(["log1"], TestContext.Current.CancellationToken);
        _ = result.ShouldNotBeNull();
        result.CompletedUtc.ShouldBeNull();
        result.BytesTransferred.ShouldBeNull();
        result.Error.ShouldBeNull();
    }

    [Fact]
    public async Task LogTransferWithAllFieldsPopulated()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var log = new TransferLog("PlaceholderAccountId", "log1", TransferType.Download, "item1", now.AddMinutes(-10), now, TransferStatus.Failed, 512, "Network timeout");
        await repository.LogTransferAsync("PlaceholderAccountId", log, CancellationToken.None);
        using var assertionContext = new AppDbContext(options);
        TransferLog? result = await assertionContext.TransferLogs.FindAsync(["log1"], TestContext.Current.CancellationToken);
        _ = result.ShouldNotBeNull();
        _ = result.CompletedUtc.ShouldNotBeNull();
        result.BytesTransferred.ShouldBe(512);
        result.Error.ShouldBe("Network timeout");
    }

    [Fact]
    public async Task LogMultipleTransferTypes()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        TestDbContextFactory factory = new(options);
        var repository = new EfSyncRepository(factory, NullLogger<EfSyncRepository>.Instance);
        TransferLog[] logs =
        [
            new TransferLog("PlaceholderAccountId", "log1", TransferType.Download, "item1", DateTimeOffset.UtcNow, null, TransferStatus.Pending, null, null),
            new TransferLog("PlaceholderAccountId", "log2", TransferType.Upload, "item2", DateTimeOffset.UtcNow, null, TransferStatus.InProgress, null, null),
            new TransferLog("PlaceholderAccountId", "log3", TransferType.Delete, "item3", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, TransferStatus.Success, null, null)
        ];
        foreach(TransferLog? log in logs)
        {
            await repository.LogTransferAsync("PlaceholderAccountId", log, CancellationToken.None);
        }

        using var assertionContext = new AppDbContext(options);
        List<TransferLog> allLogs = await assertionContext.TransferLogs.ToListAsync(TestContext.Current.CancellationToken);
        allLogs.Count.ShouldBe(3);
        allLogs.ShouldContain(l => l.Type == TransferType.Download);
        allLogs.ShouldContain(l => l.Type == TransferType.Upload);
        allLogs.ShouldContain(l => l.Type == TransferType.Delete);
    }
    #endregion
}
