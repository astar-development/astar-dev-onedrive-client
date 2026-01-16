using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Integration;

public sealed class SyncEngineShould : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly ISyncRepository _repo;
    private readonly IGraphClient _mockGraph;
    private readonly ITransferService _mockTransfer;
    private readonly ILogger<SyncEngine> _mockLogger;
    private readonly IDeltaPageProcessor _mockDeltaPageProcessor;
    private readonly ILocalFileScanner _mockLocalFileScanner;
    private readonly IDbContextFactory<AppDbContext> _mockDbContextFactory;
    private readonly ILogger<EfSyncRepository> _mockRepoLogger;

    public SyncEngineShould()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _ = _context.Database.EnsureCreated();

        _mockDbContextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        _ = _mockDbContextFactory.CreateDbContext().Returns(_context);
        _mockRepoLogger = Substitute.For<ILogger<EfSyncRepository>>();
        _repo = new EfSyncRepository(_mockDbContextFactory, _mockRepoLogger);
        _mockGraph = Substitute.For<IGraphClient>();
        _mockTransfer = Substitute.For<ITransferService>();
        _mockLogger = Substitute.For<ILogger<SyncEngine>>();
        _mockDeltaPageProcessor = Substitute.For<IDeltaPageProcessor>();
        _mockLocalFileScanner = Substitute.For<ILocalFileScanner>();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task InitialFullSyncAsync_WithSinglePage_SavesDeltaTokenAndProcessesTransfers()
    {
        // Arrange
        DeltaPage deltaPage = new(
            [
                new DriveItemRecord("PlaceholderAccountId","item1", "driveId1", "file1.txt", "etag1", "ctag1", 1024, DateTimeOffset.UtcNow, false, false)
            ],
            null,
            "deltaToken123"
        );

        _ = _mockGraph.GetDriveDeltaPageAsync("PlaceholderAccountId", null, Arg.Any<CancellationToken>())
            .Returns(deltaPage);

        _ = _mockTransfer.ProcessPendingDownloadsAsync("PlaceholderAccountId", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _ = _mockTransfer.ProcessPendingUploadsAsync("PlaceholderAccountId", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var engine = new SyncEngine(_mockDeltaPageProcessor, _mockLocalFileScanner, _mockTransfer, _repo, _mockLogger);

        // Act
        await engine.InitialFullSyncAsync("PlaceholderAccountId", TestContext.Current.CancellationToken);
        // Assert
        DeltaToken? savedToken = await _repo.GetDeltaTokenAsync("PlaceholderAccountId", TestContext.Current.CancellationToken);
        _ = savedToken.ShouldNotBeNull();
        savedToken.Token.ShouldBe("deltaToken123");

        await _mockTransfer.Received(1).ProcessPendingDownloadsAsync("PlaceholderAccountId", Arg.Any<CancellationToken>());
        await _mockTransfer.Received(1).ProcessPendingUploadsAsync("PlaceholderAccountId", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitialFullSyncAsync_WithMultiplePages_PaginatesAndSavesFinalDeltaToken()
    {
        // Arrange
        DeltaPage page1 = new(
            [
                new DriveItemRecord("PlaceholderAccountId","item1", "driveId1", "file1.txt", "etag1", "ctag1", 1024, DateTimeOffset.UtcNow, false, false)
            ],
            "nextLink1",
            null
        );

        DeltaPage page2 = new(
            [
                new DriveItemRecord("PlaceholderAccountId","item2", "driveId2", "file2.txt", "etag2", "ctag2", 2048, DateTimeOffset.UtcNow, false, false)
            ],
            "nextLink2",
            null
        );

        DeltaPage page3 = new(
            [
                new DriveItemRecord("PlaceholderAccountId","item3", "driveId3", "file3.txt", "etag3", "ctag3", 4096, DateTimeOffset.UtcNow, false, false)
            ],
            null,
            "finalDeltaToken"
        );

        _ = _mockGraph.GetDriveDeltaPageAsync("PlaceholderAccountId", null, Arg.Any<CancellationToken>())
            .Returns(page1);

        _ = _mockGraph.GetDriveDeltaPageAsync("PlaceholderAccountId", "nextLink1", Arg.Any<CancellationToken>())
            .Returns(page2);

        _ = _mockGraph.GetDriveDeltaPageAsync("PlaceholderAccountId", "nextLink2", Arg.Any<CancellationToken>())
            .Returns(page3);

        _ = _mockTransfer.ProcessPendingDownloadsAsync("PlaceholderAccountId", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _ = _mockTransfer.ProcessPendingUploadsAsync("PlaceholderAccountId", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var engine = new SyncEngine(_mockDeltaPageProcessor, _mockLocalFileScanner, _mockTransfer, _repo, _mockLogger);

        // Act
        await engine.InitialFullSyncAsync("PlaceholderAccountId", TestContext.Current.CancellationToken);

        // Assert
        DeltaToken? savedToken = await _repo.GetDeltaTokenAsync("PlaceholderAccountId",TestContext.Current.CancellationToken);
        _ = savedToken.ShouldNotBeNull();
        savedToken.Token.ShouldBe("finalDeltaToken");

        // Verify all 3 drive items were saved
        IEnumerable<DriveItemRecord> items = await _repo.GetPendingDownloadsAsync("PlaceholderAccountId", 100, 0, TestContext.Current.CancellationToken);
        var itemList = items.ToList();
        itemList.Count.ShouldBe(3);
    }

    [Fact]
    public async Task InitialFullSyncAsync_EmitsProgressEvents()
    {
        // Arrange
        DeltaPage deltaPage = new(
            [
                new DriveItemRecord("PlaceholderAccountId","item1", "driveId1", "file1.txt", "etag1", "ctag1", 1024, DateTimeOffset.UtcNow, false, false)
            ],
            null,
            "deltaToken123"
        );

        _ = _mockGraph.GetDriveDeltaPageAsync("PlaceholderAccountId", null, Arg.Any<CancellationToken>())
            .Returns(deltaPage);

        _ = _mockTransfer.ProcessPendingDownloadsAsync("PlaceholderAccountId",Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _ = _mockTransfer.ProcessPendingUploadsAsync("PlaceholderAccountId",Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var engine = new SyncEngine(_mockDeltaPageProcessor, _mockLocalFileScanner, _mockTransfer, _repo, _mockLogger);

        List<SyncProgress> progressEvents = [];
        _ = engine.Progress.Subscribe(progressEvents.Add);

        // Act
        await engine.InitialFullSyncAsync("PlaceholderAccountId", TestContext.Current.CancellationToken);

        // Assert
        progressEvents.Count.ShouldBeGreaterThan(0);
        progressEvents[0].CurrentOperationMessage.ShouldContain("Starting initial full sync");
        progressEvents[^1].CurrentOperationMessage.ShouldContain("completed");
    }

    [Fact]
    public async Task IncrementalSyncAsync_WithExistingToken_UpdatesDeltaToken()
    {
        // Arrange - save initial token
        DeltaToken initialToken = new("PlaceholderAccountId", Guid.NewGuid().ToString(), "oldToken", DateTimeOffset.UtcNow.AddHours(-1));
        await _repo.SaveOrUpdateDeltaTokenAsync("PlaceholderAccountId", initialToken, TestContext.Current.CancellationToken);

        DeltaPage deltaPage = new(
            [
                new DriveItemRecord("PlaceholderAccountId","item1", "driveId1", "file1.txt", "etag1", "ctag1", 1024, DateTimeOffset.UtcNow, false, false)
            ],
            null,
            "newDeltaToken"
        );

        _ = _mockGraph.GetDriveDeltaPageAsync("PlaceholderAccountId","oldToken", Arg.Any<CancellationToken>())
            .Returns(deltaPage);

        _ = _mockTransfer.ProcessPendingDownloadsAsync("PlaceholderAccountId",Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _ = _mockTransfer.ProcessPendingUploadsAsync("PlaceholderAccountId",Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var engine = new SyncEngine(_mockDeltaPageProcessor, _mockLocalFileScanner, _mockTransfer, _repo, _mockLogger);

        // Act
        await engine.IncrementalSyncAsync("PlaceholderAccountId", initialToken, TestContext.Current.CancellationToken);

        // Assert
        DeltaToken? updatedToken = await _repo.GetDeltaTokenAsync("PlaceholderAccountId", TestContext.Current.CancellationToken);
        _ = updatedToken.ShouldNotBeNull();
        updatedToken.Token.ShouldBe("newDeltaToken");
        updatedToken.Id.ShouldBe(initialToken.Id); // Same record, updated

        await _mockTransfer.Received(1).ProcessPendingDownloadsAsync("PlaceholderAccountId",Arg.Any<CancellationToken>());
        await _mockTransfer.Received(1).ProcessPendingUploadsAsync("PlaceholderAccountId",Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IncrementalSyncAsync_WithoutDeltaToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var engine = new SyncEngine(_mockDeltaPageProcessor, _mockLocalFileScanner, _mockTransfer, _repo, _mockLogger);

        // Act & Assert
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await engine.IncrementalSyncAsync("PlaceholderAccountId",null!, TestContext.Current.CancellationToken)
        );

        exception.Message.ShouldContain("Delta token missing");
        exception.Message.ShouldContain("run initial sync first");
    }

    [Fact]
    public async Task IncrementalSyncAsync_WithNullDeltaLink_DoesNotUpdateToken()
    {
        // Arrange - save initial token
        DeltaToken initialToken = new("PlaceholderAccountId", Guid.NewGuid().ToString(), "existingToken", DateTimeOffset.UtcNow.AddHours(-1));
        await _repo.SaveOrUpdateDeltaTokenAsync("PlaceholderAccountId",initialToken, TestContext.Current.CancellationToken);

        DeltaPage deltaPage = new(
            [
                new DriveItemRecord("PlaceholderAccountId","item1", "driveId1", "file1.txt", "etag1", "ctag1", 1024, DateTimeOffset.UtcNow, false, false)
            ],
            null,
            null // No new delta token
        );

        _ = _mockGraph.GetDriveDeltaPageAsync("PlaceholderAccountId","existingToken", Arg.Any<CancellationToken>())
            .Returns(deltaPage);

        _ = _mockTransfer.ProcessPendingDownloadsAsync("PlaceholderAccountId",Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _ = _mockTransfer.ProcessPendingUploadsAsync("PlaceholderAccountId",Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var engine = new SyncEngine(_mockDeltaPageProcessor, _mockLocalFileScanner, _mockTransfer, _repo, _mockLogger);

        // Act
        await engine.IncrementalSyncAsync("PlaceholderAccountId",initialToken, TestContext.Current.CancellationToken);

        // Assert - token should remain unchanged
        DeltaToken? token = await _repo.GetDeltaTokenAsync("PlaceholderAccountId",TestContext.Current.CancellationToken);
        _ = token.ShouldNotBeNull();
        token.Token.ShouldBe("existingToken");
    }

    [Fact]
    public async Task IncrementalSyncAsync_EmitsProgressEvents()
    {
        // Arrange - save initial token
        DeltaToken initialToken = new("PlaceholderAccountId", Guid.NewGuid().ToString(), "token", DateTimeOffset.UtcNow.AddHours(-1));
        await _repo.SaveOrUpdateDeltaTokenAsync("PlaceholderAccountId",initialToken, TestContext.Current.CancellationToken);

        DeltaPage deltaPage = new([], null, "newToken");

        _ = _mockGraph.GetDriveDeltaPageAsync("PlaceholderAccountId","token", Arg.Any<CancellationToken>())
            .Returns(deltaPage);

        _ = _mockTransfer.ProcessPendingDownloadsAsync("PlaceholderAccountId",Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _ = _mockTransfer.ProcessPendingUploadsAsync("PlaceholderAccountId",Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var engine = new SyncEngine(_mockDeltaPageProcessor, _mockLocalFileScanner, _mockTransfer, _repo, _mockLogger);

        List<SyncProgress> progressEvents = [];
        _ = engine.Progress.Subscribe(progressEvents.Add);

        // Act
        await engine.IncrementalSyncAsync("PlaceholderAccountId", initialToken, TestContext.Current.CancellationToken);

        // Assert
        progressEvents.Count.ShouldBeGreaterThan(0);
        progressEvents[0].CurrentOperationMessage.ShouldContain("Starting incremental sync");
        progressEvents[^1].CurrentOperationMessage.ShouldContain("completed");
    }

    [Fact]
    public async Task InitialFullSyncAsync_WithEmptyDeltaPage_SavesDeltaTokenWithoutItems()
    {
        // Arrange
        DeltaPage emptyPage = new([], null, "emptyDeltaToken");

        _ = _mockGraph.GetDriveDeltaPageAsync("PlaceholderAccountId",null, Arg.Any<CancellationToken>())
            .Returns(emptyPage);

        _ = _mockTransfer.ProcessPendingDownloadsAsync("PlaceholderAccountId",Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _ = _mockTransfer.ProcessPendingUploadsAsync("PlaceholderAccountId",Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var engine = new SyncEngine(_mockDeltaPageProcessor, _mockLocalFileScanner, _mockTransfer, _repo, _mockLogger);

        // Act
        await engine.InitialFullSyncAsync("PlaceholderAccountId",TestContext.Current.CancellationToken);

        // Assert
        DeltaToken? savedToken = await _repo.GetDeltaTokenAsync("PlaceholderAccountId",TestContext.Current.CancellationToken);
        _ = savedToken.ShouldNotBeNull();
        savedToken.Token.ShouldBe("emptyDeltaToken");

        IEnumerable<DriveItemRecord> items = await _repo.GetPendingDownloadsAsync("PlaceholderAccountId",100, 0, TestContext.Current.CancellationToken);
        items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Progress_IsObservable_AllowsMultipleSubscribers()
    {
        // Arrange
        DeltaPage deltaPage = new([], null, "token");

        _ = _mockGraph.GetDriveDeltaPageAsync("PlaceholderAccountId",null, Arg.Any<CancellationToken>())
            .Returns(deltaPage);

        _ = _mockTransfer.ProcessPendingDownloadsAsync("PlaceholderAccountId",Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _ = _mockTransfer.ProcessPendingUploadsAsync("PlaceholderAccountId",Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var engine = new SyncEngine(_mockDeltaPageProcessor, _mockLocalFileScanner, _mockTransfer, _repo, _mockLogger);

        List<SyncProgress> subscriber1Events = [];
        List<SyncProgress> subscriber2Events = [];

        _ = engine.Progress.Subscribe(subscriber1Events.Add);
        _ = engine.Progress.Subscribe(subscriber2Events.Add);

        // Act
        await engine.InitialFullSyncAsync("PlaceholderAccountId",TestContext.Current.CancellationToken);

        // Assert
        subscriber1Events.Count.ShouldBeGreaterThan(0);
        subscriber2Events.Count.ShouldBeGreaterThan(0);
        subscriber1Events.Count.ShouldBe(subscriber2Events.Count);
    }

    [Fact]
    public async Task InitialFullSyncAsync_AppliesDriveItemsToRepository()
    {
        // Arrange
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        DeltaPage deltaPage = new(
            [
                new DriveItemRecord("PlaceholderAccountId","item1", "driveId1", "docs/file1.txt", "etag1", "ctag1", 1024, timestamp, false, false),
                new DriveItemRecord("PlaceholderAccountId","item2", "driveId2", "docs/file2.txt", "etag2", "ctag2", 2048, timestamp, false, false),
                new DriveItemRecord("PlaceholderAccountId","item3", "driveId3", "photos/image.jpg", "etag3", "ctag3", 4096, timestamp, false, false)
            ],
            null,
            "deltaToken"
        );

        _ = _mockGraph.GetDriveDeltaPageAsync("PlaceholderAccountId",null, Arg.Any<CancellationToken>())
            .Returns(deltaPage);

        _ = _mockTransfer.ProcessPendingDownloadsAsync("PlaceholderAccountId",Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _ = _mockTransfer.ProcessPendingUploadsAsync("PlaceholderAccountId",Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var engine = new SyncEngine(_mockDeltaPageProcessor, _mockLocalFileScanner, _mockTransfer, _repo, _mockLogger);

        // Act
        await engine.InitialFullSyncAsync("PlaceholderAccountId",TestContext.Current.CancellationToken);

        // Assert
        IEnumerable<DriveItemRecord> items = await _repo.GetPendingDownloadsAsync("PlaceholderAccountId",100, 0, TestContext.Current.CancellationToken);
        var itemList = items.ToList();

        itemList.Count.ShouldBe(3);
        itemList.ShouldContain(item => item.RelativePath == "docs/file1.txt");
        itemList.ShouldContain(item => item.RelativePath == "docs/file2.txt");
        itemList.ShouldContain(item => item.RelativePath == "photos/image.jpg");
    }
}
