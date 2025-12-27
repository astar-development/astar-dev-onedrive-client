using System.IO.Abstractions;
using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Infrastructure.FileSystem;
using FileSystemImpl = System.IO.Abstractions.FileSystem;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Integration.FileSystem;

public sealed class LocalFileSystemAdapterShould : IDisposable
{
    private readonly string _testRoot;

    public LocalFileSystemAdapterShould()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), $"LocalFileSystemAdapterTests_{Guid.NewGuid()}");
        _ = Directory.CreateDirectory(_testRoot);
    }

    public void Dispose()
    {
        if(Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    [Fact]
    public void ReturnFileInfoForExistingFile()
    {
        var relativePath = "subfolder/test.txt";
        var fullPath = Path.Combine(_testRoot, relativePath);
        var adapter = new LocalFileSystemAdapter(_testRoot, new FileSystemImpl());
        _ = Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, "content");

        IFileInfo result = adapter.GetFileInfo(relativePath);

        result.Exists.ShouldBeTrue();
        result.Name.ShouldBe("test.txt");
    }

    [Fact]
    public void ReturnFileInfoForNonExistentFile()
    {
        var relativePath = "nonexistent.txt";
        var adapter = new LocalFileSystemAdapter(_testRoot, new FileSystemImpl());

        IFileInfo result = adapter.GetFileInfo(relativePath);

        result.Exists.ShouldBeFalse();
    }

    [Fact]
    public void HandleLeadingSlashesInRelativePath()
    {
        var adapter = new LocalFileSystemAdapter(_testRoot, new FileSystemImpl());
        var relativePath = "/subfolder/test.txt";
        var fullPath = Path.Combine(_testRoot, "subfolder", "test.txt");
        _ = Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, "content");

        IFileInfo result = adapter.GetFileInfo(relativePath);

        result.Exists.ShouldBeTrue();
    }

    [Fact]
    public async Task WriteFileAndCreateDirectoryIfNotExists()
    {
        var relativePath = "subfolder/newfile.txt";
        var fullPath = Path.Combine(_testRoot, relativePath);
        var adapter = new LocalFileSystemAdapter(_testRoot, new FileSystemImpl());
        var content = new MemoryStream("test content"u8.ToArray());

        await adapter.WriteFileAsync(relativePath, content, CancellationToken.None);

        File.Exists(fullPath).ShouldBeTrue();
        var writtenContent = await File.ReadAllTextAsync(fullPath, TestContext.Current.CancellationToken);
        writtenContent.ShouldBe("test content");
    }

    [Fact]
    public async Task WriteFileToExistingDirectory()
    {
        var relativePath = "existing/file.txt";
        var fullPath = Path.Combine(_testRoot, relativePath);
        var adapter = new LocalFileSystemAdapter(_testRoot, new FileSystemImpl());
        _ = Directory.CreateDirectory(Path.Combine(_testRoot, "existing"));
        var content = new MemoryStream("data"u8.ToArray());

        await adapter.WriteFileAsync(relativePath, content, CancellationToken.None);

        File.Exists(fullPath).ShouldBeTrue();
    }

    [Fact]
    public async Task OverwriteExistingFile()
    {
        var relativePath = "overwrite.txt";
        var fullPath = Path.Combine(_testRoot, relativePath);
        var adapter = new LocalFileSystemAdapter(_testRoot, new FileSystemImpl());
        await File.WriteAllTextAsync(fullPath, "old content", TestContext.Current.CancellationToken);

        var newContent = new MemoryStream("new content"u8.ToArray());
        await adapter.WriteFileAsync(relativePath, newContent, CancellationToken.None);

        var result = await File.ReadAllTextAsync(fullPath, TestContext.Current.CancellationToken);
        result.ShouldBe("new content");
    }

    [Fact]
    public async Task OpenReadReturnStreamForExistingFile()
    {
        var relativePath = "read.txt";
        var fullPath = Path.Combine(_testRoot, relativePath);
        var adapter = new LocalFileSystemAdapter(_testRoot, new FileSystemImpl());
        await File.WriteAllTextAsync(fullPath, "file content", TestContext.Current.CancellationToken);

        Stream? result = await adapter.OpenReadAsync(relativePath, CancellationToken.None);

        _ = result.ShouldNotBeNull();
        using var reader = new StreamReader(result);
        var content = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        content.ShouldBe("file content");
    }

    [Fact]
    public async Task OpenReadReturnNullForNonExistentFile()
    {
        var relativePath = "missing.txt";
        var adapter = new LocalFileSystemAdapter(_testRoot, new FileSystemImpl());

        Stream? result = await adapter.OpenReadAsync(relativePath, CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteExistingFile()
    {
        var relativePath = "delete.txt";
        var fullPath = Path.Combine(_testRoot, relativePath);
        var adapter = new LocalFileSystemAdapter(_testRoot, new FileSystemImpl());
        await File.WriteAllTextAsync(fullPath, "content", TestContext.Current.CancellationToken);

        await adapter.DeleteFileAsync(relativePath, CancellationToken.None);

        File.Exists(fullPath).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteNonExistentFileWithoutError()
    {
        var relativePath = "nonexistent.txt";
        var adapter = new LocalFileSystemAdapter(_testRoot, new FileSystemImpl());

        Exception? exception = await Record.ExceptionAsync(async () =>
            await adapter.DeleteFileAsync(relativePath, CancellationToken.None));

        exception.ShouldBeNull();
    }

    [Fact]
    public async Task EnumerateFilesReturnEmptyListWhenDirectoryDoesNotExist()
    {
        var nonExistentRoot = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid()}");
        var adapter = new LocalFileSystemAdapter(nonExistentRoot, new FileSystemImpl());

        IEnumerable<LocalFileInfo> result = await adapter.EnumerateFilesAsync(CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task EnumerateFilesReturnAllFilesRecursively()
    {
        var adapter = new LocalFileSystemAdapter(_testRoot, new FileSystemImpl());
        _ = Directory.CreateDirectory(Path.Combine(_testRoot, "subfolder"));
        await File.WriteAllTextAsync(Path.Combine(_testRoot, "file1.txt"), "content1", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_testRoot, "subfolder", "file2.txt"), "content2", TestContext.Current.CancellationToken);

        IEnumerable<LocalFileInfo> result = await adapter.EnumerateFilesAsync(CancellationToken.None);

        result.Count().ShouldBe(2);
        result.ShouldContain(f => f.RelativePath == "file1.txt");
        result.ShouldContain(f => f.RelativePath == Path.Combine("subfolder", "file2.txt"));
    }

    [Fact]
    public async Task EnumerateFilesPopulateLocalFileInfoProperties()
    {
        var adapter = new LocalFileSystemAdapter(_testRoot, new FileSystemImpl());
        var testFile = Path.Combine(_testRoot, "test.txt");
        await File.WriteAllTextAsync(testFile, "content", TestContext.Current.CancellationToken);
        var fileInfo = new FileInfo(testFile);

        IEnumerable<LocalFileInfo> result = await adapter.EnumerateFilesAsync(CancellationToken.None);

        LocalFileInfo item = result.Single();
        item.RelativePath.ShouldBe("test.txt");
        item.Size.ShouldBe(7); // "content" = 7 bytes
        item.LastWriteUtc.ShouldBe(fileInfo.LastWriteTimeUtc, TimeSpan.FromSeconds(1));
        item.Hash.ShouldBeNull();
    }
}
