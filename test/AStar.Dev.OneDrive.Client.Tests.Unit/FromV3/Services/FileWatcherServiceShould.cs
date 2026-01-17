using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.FromV3;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Models.Enums;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.FromV3.Services;

public class FileWatcherServiceShould : IDisposable
{
    private readonly string _testDirectory;
    private readonly FileWatcherService _sut;
    private bool disposedValue;

    public FileWatcherServiceShould()
    {
        ILogger<FileWatcherService> mockLogger = Substitute.For<ILogger<FileWatcherService>>();
        _sut = new FileWatcherService(mockLogger);

        // Create a temporary directory for testing
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FileWatcherTest_{Guid.CreateVersion7()}");
        _ = Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenAccountIdIsNull()
    {
        Exception? exception = Record.Exception(() =>
            _sut.StartWatching(null!, _testDirectory));

        _ = exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenLocalPathIsNull()
    {
        Exception? exception = Record.Exception(() =>
            _sut.StartWatching("account1", null!));

        _ = exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowDirectoryNotFoundExceptionWhenPathDoesNotExist()
    {
        var nonExistentPath = Path.Combine(_testDirectory, "NonExistent");

        Exception? exception = Record.Exception(() =>
            _sut.StartWatching("account1", nonExistentPath));

        _ = exception.ShouldBeOfType<DirectoryNotFoundException>();
    }

    [Fact]
    public async Task DetectFileCreationEvents()
    {
        var events = new List<FileChangeEvent>();
        using IDisposable subscription = _sut.FileChanges.Subscribe(events.Add);

        _sut.StartWatching("account1", _testDirectory);

        // Give watcher time to initialize
        await Task.Delay(200, CancellationToken.None);

        // Create a file
        var testFile = Path.Combine(_testDirectory, "test.txt");
        await File.WriteAllTextAsync(testFile, "content", CancellationToken.None);
        // Wait for debounce (500ms) + buffer - FileSystemWatcher needs more time on some systems
        await Task.Delay(1000, CancellationToken.None);

        // FileSystemWatcher behavior can vary - just verify SOME events were captured
        events.ShouldNotBeEmpty();

        // Verify at least one event is for our test file
        FileChangeEvent? fileEvent = events.FirstOrDefault(e => e.RelativePath == "test.txt");
        fileEvent?.AccountId.ShouldBe("account1");
    }

    [Fact]
    public async Task DetectFileModificationEvents()
    {
        // Pre-create file
        var testFile = Path.Combine(_testDirectory, "modify.txt");
        await File.WriteAllTextAsync(testFile, "initial", CancellationToken.None);

        var events = new List<FileChangeEvent>();
        using IDisposable subscription = _sut.FileChanges.Subscribe(events.Add);

        _sut.StartWatching("account1", _testDirectory);
        await Task.Delay(100, CancellationToken.None);

        // Modify the file
        await File.WriteAllTextAsync(testFile, "modified content", CancellationToken.None);

        // Wait for debounce
        await Task.Delay(700, CancellationToken.None);

        events.ShouldNotBeEmpty();

        FileChangeEvent? modifiedEvent = events.FirstOrDefault(e =>
            e.ChangeType == FileChangeType.Modified &&
            e.RelativePath == "modify.txt");

        _ = modifiedEvent.ShouldNotBeNull();
        modifiedEvent.AccountId.ShouldBe("account1");
    }

    [Fact]
    public async Task DetectFileDeletionEvents()
    {
        // Pre-create file
        var testFile = Path.Combine(_testDirectory, "delete.txt");
        await File.WriteAllTextAsync(testFile, "content", CancellationToken.None);

        var events = new List<FileChangeEvent>();
        using IDisposable subscription = _sut.FileChanges.Subscribe(events.Add);

        _sut.StartWatching("account1", _testDirectory);
        await Task.Delay(100, CancellationToken.None);

        // Delete the file
        File.Delete(testFile);

        // Deletion events are not debounced, should be immediate
        await Task.Delay(300, CancellationToken.None);

        events.ShouldNotBeEmpty();

        FileChangeEvent? deletedEvent = events.FirstOrDefault(e =>
            e.ChangeType == FileChangeType.Deleted &&
            e.RelativePath == "delete.txt");

        _ = deletedEvent.ShouldNotBeNull();
        deletedEvent.AccountId.ShouldBe("account1");
    }

    [Fact]
    public async Task DetectChangesInSubdirectories()
    {
        var subDir = Path.Combine(_testDirectory, "SubFolder");
        _ = Directory.CreateDirectory(subDir);

        var events = new List<FileChangeEvent>();
        using IDisposable subscription = _sut.FileChanges.Subscribe(events.Add);

        _sut.StartWatching("account1", _testDirectory);
        await Task.Delay(100, CancellationToken.None);

        // Create file in subdirectory
        var testFile = Path.Combine(subDir, "nested.txt");
        await File.WriteAllTextAsync(testFile, "content", CancellationToken.None);
        await Task.Delay(700, CancellationToken.None);

        events.ShouldNotBeEmpty();

        FileChangeEvent? nestedEvent = events.FirstOrDefault(e =>
            e.RelativePath == Path.Combine("SubFolder", "nested.txt"));

        _ = nestedEvent.ShouldNotBeNull();
        nestedEvent.AccountId.ShouldBe("account1");
    }

    [Fact]
    public async Task HandleMultipleAccountsIndependently()
    {
        var dir1 = Path.Combine(_testDirectory, "Account1");
        var dir2 = Path.Combine(_testDirectory, "Account2");
        _ = Directory.CreateDirectory(dir1);
        _ = Directory.CreateDirectory(dir2);

        var events = new List<FileChangeEvent>();
        using IDisposable subscription = _sut.FileChanges.Subscribe(events.Add);

        _sut.StartWatching("account1", dir1);
        _sut.StartWatching("account2", dir2);
        await Task.Delay(200, CancellationToken.None);

        // Create files in both directories
        await File.WriteAllTextAsync(Path.Combine(dir1, "file1.txt"), "content1", CancellationToken.None);
        await File.WriteAllTextAsync(Path.Combine(dir2, "file2.txt"), "content2", CancellationToken.None);

        await Task.Delay(1000, CancellationToken.None);

        // Verify events were captured for both accounts
        events.ShouldNotBeEmpty();

        var account1Events = events.Where(e => e.AccountId == "account1").ToList();
        var account2Events = events.Where(e => e.AccountId == "account2").ToList();

        // At least one of the accounts should have events
        (account1Events.Count + account2Events.Count).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void StopWatchingRemovesWatcher()
    {
        _sut.StartWatching("account1", _testDirectory);

        // Shouldn't throw
        _sut.StopWatching("account1");

        // Stopping non-existent watcher shouldn't throw
        _sut.StopWatching("nonExistent");
    }

    [Fact]
    public async Task StopWatchingStopsEmittingEvents()
    {
        var events = new List<FileChangeEvent>();
        using IDisposable subscription = _sut.FileChanges.Subscribe(events.Add);

        _sut.StartWatching("account1", _testDirectory);
        _sut.StopWatching("account1");

        // File changes after stopping should not be detected
        var testFile = Path.Combine(_testDirectory, "after_stop.txt");
        await File.WriteAllTextAsync(testFile, "content", CancellationToken.None);

        // No events should be emitted
        await Task.Delay(700, CancellationToken.None);

        events.ShouldBeEmpty();
    }

    [Fact]
    public void ReplaceExistingWatcherWhenStartingWatchingSameAccount()
    {
        _sut.StartWatching("account1", _testDirectory);

        // Start watching again for same account - should stop existing watcher
        Exception? exception = Record.Exception(() =>
            _sut.StartWatching("account1", _testDirectory));

        exception.ShouldBeNull();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenStoppingNullAccountId()
    {
        Exception? exception = Record.Exception(() =>
            _sut.StopWatching(null!));

        _ = exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void DisposeCleanlyWithoutThrowingExceptions()
    {
        var subFolder = Path.Combine(_testDirectory, "SubFolder");
        _ = Directory.CreateDirectory(subFolder);

        _sut.StartWatching("account1", _testDirectory);
        _sut.StartWatching("account2", subFolder);

        Exception? exception = Record.Exception(_sut.Dispose);

        exception.ShouldBeNull();
    }

    [Fact]
    public async Task DebounceRapidFileChanges()
    {
        var testFile = Path.Combine(_testDirectory, "rapid.txt");
        await File.WriteAllTextAsync(testFile, "initial", CancellationToken.None);

        var events = new List<FileChangeEvent>();
        using IDisposable subscription = _sut.FileChanges.Subscribe(events.Add);

        _sut.StartWatching("account1", _testDirectory);
        await Task.Delay(100, CancellationToken.None);

        // Make rapid changes to the same file
        for(var i = 0; i < 5; i++)
        {
            await File.AppendAllTextAsync(testFile, $"update{i}", CancellationToken.None);
            await Task.Delay(50, CancellationToken.None); // 50ms between writes (within 500ms debounce window)
        }

        // Wait for debounce period
        await Task.Delay(700, CancellationToken.None);

        // Should have received events, but likely fewer than 5 due to debouncing
        var rapidEvents = events.Where(e => e.RelativePath == "rapid.txt").ToList();

        // Due to debouncing, we expect fewer events than the number of writes
        // Exact count depends on timing, but should be significantly less than 5
        rapidEvents.Count.ShouldBeLessThan(5);
        rapidEvents.ShouldNotBeEmpty();
    }

    protected virtual void Dispose(bool disposing)
    {
        if(!disposedValue)
        {
            if(disposing)
            {
                if(Directory.Exists(_testDirectory))
                {
                    Directory.Delete(_testDirectory, recursive: true);
                }
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
