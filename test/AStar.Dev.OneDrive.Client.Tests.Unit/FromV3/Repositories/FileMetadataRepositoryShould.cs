using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Models.Enums;
using AStar.Dev.OneDrive.Client.FromV3.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.FromV3.Repositories;

public class FileMetadataRepositoryShould
{
    [Fact]
    public async Task GetFilesByAccountIdCorrectly()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc1.txt"), CancellationToken.None);
        await repository.AddAsync(CreateFileMetadata("file2", "acc1", "/doc2.txt"), CancellationToken.None);
        await repository.AddAsync(CreateFileMetadata("file3", "acc2", "/doc3.txt"), CancellationToken.None);
        IReadOnlyList<FileMetadata> result = await repository.GetByAccountIdAsync("acc1", CancellationToken.None);

        result.Count.ShouldBe(2);
        result.ShouldContain(f => f.Id == "file1");
        result.ShouldContain(f => f.Id == "file2");
    }

    [Fact]
    public async Task GetFileByIdCorrectly()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc.txt"), CancellationToken.None);

        FileMetadata? result = await repository.GetByIdAsync("file1", CancellationToken.None);

        _ = result.ShouldNotBeNull();
        result.Id.ShouldBe("file1");
        result.Path.ShouldBe("/doc.txt");
    }

    [Fact]
    public async Task ReturnNullWhenFileNotFoundById()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);

        FileMetadata? result = await repository.GetByIdAsync("nonexistent", CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetFileByPathCorrectly()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/docs/file.txt"), CancellationToken.None);

        FileMetadata? result = await repository.GetByPathAsync("acc1", "/docs/file.txt", CancellationToken.None);

        _ = result.ShouldNotBeNull();
        result.Id.ShouldBe("file1");
    }

    [Fact]
    public async Task ReturnNullWhenFileNotFoundByPath()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);

        FileMetadata? result = await repository.GetByPathAsync("acc1", "/nonexistent.txt", CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetFilesByStatusCorrectly()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc1.txt", FileSyncStatus.Synced), CancellationToken.None);
        await repository.AddAsync(CreateFileMetadata("file2", "acc1", "/doc2.txt", FileSyncStatus.PendingUpload), CancellationToken.None);
        await repository.AddAsync(CreateFileMetadata("file3", "acc1", "/doc3.txt", FileSyncStatus.Conflict), CancellationToken.None);

        IReadOnlyList<FileMetadata> result = await repository.GetByStatusAsync("acc1", FileSyncStatus.PendingUpload, CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe("file2");
    }

    [Fact]
    public async Task AddFileMetadataSuccessfully()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        FileMetadata file = CreateFileMetadata("file1", "acc1", "/doc.txt");

        await repository.AddAsync(file, CancellationToken.None);

        FileMetadata? result = await repository.GetByIdAsync("file1", CancellationToken.None);
        _ = result.ShouldNotBeNull();
        result.Name.ShouldBe("doc.txt");
    }

    [Fact]
    public async Task UpdateFileMetadataSuccessfully()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc.txt", FileSyncStatus.PendingUpload), CancellationToken.None);

        var updated = new FileMetadata("file1", "acc1", "doc.txt", "/doc.txt", 2048, DateTime.UtcNow, @"C:\local\doc.txt", "newtag", "newetag", "newhash", FileSyncStatus.Synced, SyncDirection.Upload);
        await repository.UpdateAsync(updated, CancellationToken.None);

        FileMetadata? result = await repository.GetByIdAsync("file1", CancellationToken.None);
        _ = result.ShouldNotBeNull();
        result.SyncStatus.ShouldBe(FileSyncStatus.Synced);
        result.Size.ShouldBe(2048);
        result.LastSyncDirection.ShouldBe(SyncDirection.Upload);
    }

    [Fact]
    public async Task ThrowExceptionWhenUpdatingNonExistentFile()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        FileMetadata file = CreateFileMetadata("nonexistent", "acc1", "/doc.txt");

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await repository.UpdateAsync(file)
        );

        exception.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task DeleteFileMetadataSuccessfully()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc.txt"), CancellationToken.None);

        await repository.DeleteAsync("file1", CancellationToken.None);

        FileMetadata? result = await repository.GetByIdAsync("file1", CancellationToken.None);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAllFilesForAccountSuccessfully()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc1.txt"), CancellationToken.None);
        await repository.AddAsync(CreateFileMetadata("file2", "acc1", "/doc2.txt"), CancellationToken.None);
        await repository.AddAsync(CreateFileMetadata("file3", "acc2", "/doc3.txt"), CancellationToken.None);

        await repository.DeleteByAccountIdAsync("acc1", CancellationToken.None);

        IReadOnlyList<FileMetadata> acc1Files = await repository.GetByAccountIdAsync("acc1", CancellationToken.None);
        IReadOnlyList<FileMetadata> acc2Files = await repository.GetByAccountIdAsync("acc2", CancellationToken.None);
        acc1Files.ShouldBeEmpty();
        acc2Files.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SaveBatchUpsertFilesCorrectly()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/old.txt", FileSyncStatus.Synced), CancellationToken.None);

        FileMetadata[] batchFiles =
        [
            CreateFileMetadata("file1", "acc1", "/old.txt", FileSyncStatus.PendingUpload),
            CreateFileMetadata("file2", "acc1", "/new.txt", FileSyncStatus.PendingDownload)
        ];
        await repository.SaveBatchAsync(batchFiles, CancellationToken.None);

        IReadOnlyList<FileMetadata> allFiles = await repository.GetByAccountIdAsync("acc1", CancellationToken.None);
        allFiles.Count.ShouldBe(2);

        FileMetadata? file1 = await repository.GetByIdAsync("file1", CancellationToken.None);
        _ = file1.ShouldNotBeNull();
        file1.SyncStatus.ShouldBe(FileSyncStatus.PendingUpload);
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionForNullParameters()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);

        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await repository.AddAsync(null!));
        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await repository.GetByAccountIdAsync(null!));
        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await repository.GetByIdAsync(null!));
    }

    private static FileMetadata CreateFileMetadata(string id, string accountId, string path, FileSyncStatus status = FileSyncStatus.Synced)
        => new(id, accountId, Path.GetFileName(path), path, 1024, DateTime.UtcNow, $@"C:\local{path}", "ctag", "etag", "hash", status, null);

    private static AppDbContext CreateInMemoryContext()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
