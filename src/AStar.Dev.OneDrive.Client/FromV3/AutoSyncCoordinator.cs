using System.Reactive.Disposables;
using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.FromV3;

/// <inheritdoc/>
#pragma warning disable CA1848 // Use LoggerMessage delegates
#pragma warning disable CA1873 // Avoid string interpolation in logging

public sealed class AutoSyncCoordinator : IAutoSyncCoordinator
{
    private readonly IFileWatcherService _fileWatcherService;
    private readonly ISyncEngine _syncEngine;
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<AutoSyncCoordinator> _logger;
    private readonly CompositeDisposable _disposables = [];
    private readonly Dictionary<string, IDisposable> _accountSubscriptions = [];

    /// <summary>
    /// Initializes a new instance of <see cref="AutoSyncCoordinator"/>.
    /// </summary>
    /// <param name="fileWatcherService">Service for monitoring file system changes.</param>
    /// <param name="syncEngine">Sync engine for performing synchronization.</param>
    /// <param name="accountRepository">Repository for account data.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public AutoSyncCoordinator(
        IFileWatcherService fileWatcherService,
        ISyncEngine syncEngine,
        IAccountRepository accountRepository,
        ILogger<AutoSyncCoordinator> logger)
    {
        ArgumentNullException.ThrowIfNull(fileWatcherService);
        ArgumentNullException.ThrowIfNull(syncEngine);
        ArgumentNullException.ThrowIfNull(accountRepository);
        ArgumentNullException.ThrowIfNull(logger);

        _fileWatcherService = fileWatcherService;
        _syncEngine = syncEngine;
        _accountRepository = accountRepository;
        _logger = logger;
    }

    /// <summary>
    /// Starts monitoring an account's sync directory for changes.
    /// </summary>
    /// <param name="accountId">Account identifier.</param>
    /// <param name="localPath">Local sync directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// File changes are debounced with a 2-second delay to avoid excessive sync triggers
    /// when multiple files are changed rapidly.
    /// </remarks>
    public async Task StartMonitoringAsync(string accountId, string localPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(localPath);

        // Stop any existing monitoring for this account
        StopMonitoring(accountId);

        try
        {
            // Start file watcher
            _fileWatcherService.StartWatching(accountId, localPath);

            // Subscribe to file changes with debouncing (2 seconds)
            // This groups rapid file changes into a single sync operation
            IDisposable subscription = _fileWatcherService.FileChanges
                .Where(e => e.AccountId == accountId)
                .Buffer(TimeSpan.FromSeconds(2))
                .Where(changes => changes.Count > 0)
                .Subscribe(async changes =>
                {
                    _logger.LogInformation("Detected {Count} file change(s) for account {AccountId}, triggering sync",
                        changes.Count, accountId);

                    try
                    {
                        await _syncEngine.StartSyncAsync(accountId, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Auto-sync failed for account {AccountId}", accountId);
                    }
                });

            _accountSubscriptions[accountId] = subscription;

            _logger.LogInformation("Started auto-sync monitoring for account {AccountId} at {Path}",
                accountId, localPath);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Failed to start auto-sync monitoring for account {AccountId}", accountId);
            throw;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Stops monitoring an account's sync directory.
    /// </summary>
    /// <param name="accountId">Account identifier.</param>
    public void StopMonitoring(string accountId)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        if(_accountSubscriptions.Remove(accountId, out IDisposable? subscription))
        {
            subscription.Dispose();
            _fileWatcherService.StopWatching(accountId);

            _logger.LogInformation("Stopped auto-sync monitoring for account {AccountId}", accountId);
        }
    }

    /// <summary>
    /// Stops monitoring all accounts.
    /// </summary>
    public void StopAll()
    {
        foreach(var accountId in _accountSubscriptions.Keys.ToList()) StopMonitoring(accountId);
    }

    /// <summary>
    /// Disposes resources and stops all monitoring.
    /// </summary>
    public void Dispose()
    {
        StopAll();
        _disposables.Dispose();
        _logger.LogInformation("AutoSyncCoordinator disposed");
    }
}
