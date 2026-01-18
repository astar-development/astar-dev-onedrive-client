using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;

/// <summary>
/// Repository for accessing debug log entries.
/// </summary>
public interface IDebugLogRepository
{
    /// <summary>
    /// Gets debug log entries for a specific account with paging support.
    /// </summary>
    /// <param name="accountId">The account ID to filter by.</param>
    /// <param name="pageSize">Number of records to retrieve.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of debug log entries ordered by timestamp descending (newest first).</returns>
    Task<IReadOnlyList<DebugLogEntry>> GetByAccountIdAsync(string accountId, int pageSize, int skip, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all debug log entries for a specific account.
    /// </summary>
    /// <param name="accountId">The account ID to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all debug log entries for the account.</returns>
    Task<IReadOnlyList<DebugLogEntry>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of debug log entries for a specific account.
    /// </summary>
    /// <param name="accountId">The account ID to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of debug log entries for the account.</returns>
    Task<int> GetDebugLogCountByAccountIdAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all debug log entries for a specific account.
    /// </summary>
    /// <param name="accountId">The account ID to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteByAccountIdAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes debug log entries older than the specified date.
    /// </summary>
    /// <param name="olderThan">Delete entries older than this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteOlderThanAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}
