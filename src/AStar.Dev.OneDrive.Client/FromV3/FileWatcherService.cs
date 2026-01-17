using System.Reactive.Linq;
using System.Reactive.Subjects;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Models.Enums;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1848 // Use LoggerMessage delegates
#pragma warning disable CA1873 // Avoid string interpolation in logging - Acceptable for file watcher service

namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Service for monitoring local file system changes to trigger synchronization.
/// </summary>
/// <remarks>
/// Wraps FileSystemWatcher with debouncing (500ms) to handle rapid file changes
/// and partial writes. Supports monitoring multiple account directories independently.
/// </remarks>
public sealed class FileWatcherService : IFileWatcherService
{
    private readonly ILogger<FileWatcherService> _logger;
    private readonly Dictionary<string, WatcherContext> _watchers = [];
    private readonly Subject<FileChangeEvent> _fileChanges = new();
    private bool _disposed;

    /// <inheritdoc/>
    public IObservable<FileChangeEvent> FileChanges => _fileChanges.AsObservable();

    /// <summary>
    /// Initializes a new instance of <see cref="FileWatcherService"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <exception cref="ArgumentNullException">Thrown if logger is null.</exception>
    public FileWatcherService(ILogger<FileWatcherService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public void StartWatching(string accountId, string localPath)
    {
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(localPath);

        if(!Directory.Exists(localPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {localPath}");
        }

        if(_watchers.ContainsKey(accountId))
        {
            _logger.LogWarning("Already watching path for account {AccountId}. Stopping existing watcher first.", accountId);
            StopWatching(accountId);
        }

        try
        {
            var watcher = new FileSystemWatcher(localPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName
                             | NotifyFilters.DirectoryName
                             | NotifyFilters.Size
                             | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            // Debounce buffer for Changed/Created events (500ms throttle)
            var changeBuffer = new Subject<FileSystemEventArgs>();
            IDisposable subscription = changeBuffer
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(e => ProcessFileChange(accountId, localPath, e));

            // Wire up FileSystemWatcher events
            watcher.Changed += (s, e) => changeBuffer.OnNext(e);
            watcher.Created += (s, e) => changeBuffer.OnNext(e);
            watcher.Deleted += (s, e) => EmitFileChange(accountId, localPath, e, FileChangeType.Deleted);
            watcher.Renamed += (s, e) => EmitFileChange(accountId, localPath, e, FileChangeType.Renamed);
            watcher.Error += (s, e) => HandleWatcherError(accountId, e);

            _watchers[accountId] = new WatcherContext(watcher, changeBuffer, subscription);
            _logger.LogInformation("Started watching {Path} for account {AccountId}", localPath, accountId);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Failed to start watching {Path} for account {AccountId}", localPath, accountId);
            throw;
        }
    }

    /// <inheritdoc/>
    public void StopWatching(string accountId)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        if(_watchers.Remove(accountId, out WatcherContext? context))
        {
            context.Dispose();
            _logger.LogInformation("Stopped watching for account {AccountId}", accountId);
        }
    }

    private void ProcessFileChange(string accountId, string basePath, FileSystemEventArgs e)
    {
        // Determine if this is a creation or modification
        FileChangeType changeType = e.ChangeType == WatcherChangeTypes.Created
            ? FileChangeType.Created
            : FileChangeType.Modified;

        EmitFileChange(accountId, basePath, e, changeType);
    }

    private void EmitFileChange(string accountId, string basePath, FileSystemEventArgs e, FileChangeType changeType)
    {
        try
        {
            var relativePath = Path.GetRelativePath(basePath, e.FullPath);

            var changeEvent = new FileChangeEvent(
                AccountId: accountId,
                LocalPath: e.FullPath,
                RelativePath: relativePath,
                ChangeType: changeType,
                DetectedUtc: DateTime.UtcNow
            );

            _fileChanges.OnNext(changeEvent);
            _logger.LogDebug("File change detected: {ChangeType} - {RelativePath} (Account: {AccountId})",
                changeType, relativePath, accountId);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error emitting file change event for {Path}", e.FullPath);
        }
    }

    private void HandleWatcherError(string accountId, ErrorEventArgs e)
    {
        Exception exception = e.GetException();
        _logger.LogError(exception, "FileSystemWatcher error for account {AccountId}", accountId);

        // Optionally emit an error event or attempt to restart the watcher
        // For now, just log the error
    }

    /// <summary>
    /// Disposes all file system watchers and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        if(_disposed)
        {
            return;
        }

        foreach(WatcherContext context in _watchers.Values)
        {
            context.Dispose();
        }

        _watchers.Clear();
        _fileChanges.Dispose();
        _disposed = true;

        _logger.LogInformation("FileWatcherService disposed");
    }

    /// <summary>
    /// Context holding a FileSystemWatcher and its associated subscriptions.
    /// </summary>
    private sealed class WatcherContext : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly Subject<FileSystemEventArgs> _changeBuffer;
        private readonly IDisposable _subscription;

        public WatcherContext(
            FileSystemWatcher watcher,
            Subject<FileSystemEventArgs> changeBuffer,
            IDisposable subscription)
        {
            _watcher = watcher;
            _changeBuffer = changeBuffer;
            _subscription = subscription;
        }

        public void Dispose()
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _subscription.Dispose();
            _changeBuffer.Dispose();
        }
    }
}
