using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;

/// <summary>
/// Repository for managing sync session logs.
/// </summary>
public interface ISyncSessionLogRepository
{
    /// <summary>
    /// Gets all sync sessions for an account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of sync sessions.</returns>
    Task<IReadOnlyList<SyncSessionLog>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a sync session by ID.
    /// </summary>
    /// <param name="id">The session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sync session if found, otherwise null.</returns>
    Task<SyncSessionLog?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new sync session log.
    /// </summary>
    /// <param name="sessionLog">The session log to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(SyncSessionLog sessionLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing sync session log.
    /// </summary>
    /// <param name="sessionLog">The session log to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(SyncSessionLog sessionLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old sync session logs for an account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="olderThan">Delete sessions older than this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteOldSessionsAsync(string accountId, DateTime olderThan, CancellationToken cancellationToken = default);
}
