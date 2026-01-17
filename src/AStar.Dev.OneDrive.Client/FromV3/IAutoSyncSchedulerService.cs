namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Service for scheduling automatic remote sync checks for accounts.
/// </summary>
public interface IAutoSyncSchedulerService : IDisposable
{
    /// <summary>
    /// Starts the scheduler and loads all accounts with auto-sync enabled.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops all scheduled syncs.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Updates the sync schedule for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="intervalMinutes">The interval in minutes (null to disable auto-sync).</param>
    void UpdateSchedule(string accountId, int? intervalMinutes);

    /// <summary>
    /// Removes the sync schedule for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    void RemoveSchedule(string accountId);
}
