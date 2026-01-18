using System.Collections.Concurrent;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1848 // Use LoggerMessage delegates for high-performance logging
#pragma warning disable CA1873 // Argument may be expensive - acceptable for scheduler logging

namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Service for scheduling automatic remote sync checks for accounts.
/// </summary>
public sealed class AutoSyncSchedulerService : IAutoSyncSchedulerService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ISyncEngine _syncEngine;
    private readonly ILogger<AutoSyncSchedulerService> _logger;
    private readonly ConcurrentDictionary<string, System.Timers.Timer> _timers = new();
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoSyncSchedulerService"/> class.
    /// </summary>
    /// <param name="accountRepository">Repository for account data.</param>
    /// <param name="syncEngine">Sync engine for performing synchronization.</param>
    /// <param name="logger">Logger instance.</param>
    public AutoSyncSchedulerService(
        IAccountRepository accountRepository,
        ISyncEngine syncEngine,
        ILogger<AutoSyncSchedulerService> logger)
    {
        ArgumentNullException.ThrowIfNull(accountRepository);
        ArgumentNullException.ThrowIfNull(syncEngine);
        ArgumentNullException.ThrowIfNull(logger);

        _accountRepository = accountRepository;
        _syncEngine = syncEngine;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting auto-sync scheduler");

        IReadOnlyList<AccountInfo> accounts = await _accountRepository.GetAllAsync(cancellationToken);
        foreach(AccountInfo account in accounts)
            if(account is { AutoSyncIntervalMinutes: not null, IsAuthenticated: true }) UpdateSchedule(account.AccountId, account.AutoSyncIntervalMinutes.Value);

        _logger.LogInformation("Auto-sync scheduler started with {Count} scheduled accounts", _timers.Count);
    }

    /// <inheritdoc/>
    public Task StopAsync()
    {
        _logger.LogInformation("Stopping auto-sync scheduler");

        foreach((var accountId, System.Timers.Timer? timer) in _timers)
        {
            timer.Stop();
            timer.Dispose();
            _logger.LogDebug("Stopped timer for account {AccountId}", accountId);
        }

        _timers.Clear();
        _logger.LogInformation("Auto-sync scheduler stopped");

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void UpdateSchedule(string accountId, int? intervalMinutes)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        // Remove existing timer if present
        if(_timers.TryRemove(accountId, out System.Timers.Timer? existingTimer))
        {
            existingTimer.Stop();
            existingTimer.Dispose();
            _logger.LogDebug("Removed existing timer for account {AccountId}", accountId);
        }

        // Create new timer if interval is specified
        if(intervalMinutes.HasValue)
        {
            var clampedInterval = Math.Clamp(intervalMinutes.Value, 60, 1440); // 1 hour to 24 hours
            var intervalMs = clampedInterval * 60 * 1000; // Convert to milliseconds

            var timer = new System.Timers.Timer(intervalMs)
            {
                AutoReset = true
            };

            timer.Elapsed += async (sender, e) =>
            {
                try
                {
                    _logger.LogInformation("Auto-sync triggered for account {AccountId}", accountId);
                    await _syncEngine.StartSyncAsync(accountId, CancellationToken.None);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Auto-sync failed for account {AccountId}", accountId);
                }
            };

            timer.Start();
            _timers[accountId] = timer;

            _logger.LogInformation("Scheduled auto-sync for account {AccountId} every {Interval} minutes",
                accountId, clampedInterval);
        }
    }

    /// <inheritdoc/>
    public void RemoveSchedule(string accountId)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        if(_timers.TryRemove(accountId, out System.Timers.Timer? timer))
        {
            timer.Stop();
            timer.Dispose();
            _logger.LogInformation("Removed auto-sync schedule for account {AccountId}", accountId);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if(_isDisposed) return;

        StopAsync().GetAwaiter().GetResult();
        _isDisposed = true;
    }
}
