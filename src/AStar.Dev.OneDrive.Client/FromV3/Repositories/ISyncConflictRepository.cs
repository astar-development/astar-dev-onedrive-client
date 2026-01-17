using AStar.Dev.OneDrive.Client.FromV3.Models;

namespace AStar.Dev.OneDrive.Client.FromV3.Repositories;

/// <summary>
/// Repository for managing sync conflicts.
/// </summary>
public interface ISyncConflictRepository
{
    /// <summary>
    /// Gets all conflicts for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of conflicts for the account.</returns>
    Task<IReadOnlyList<SyncConflict>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unresolved conflicts for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of unresolved conflicts for the account.</returns>
    Task<IReadOnlyList<SyncConflict>> GetUnresolvedByAccountIdAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a conflict by its ID.
    /// </summary>
    /// <param name="id">The conflict identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The conflict if found, otherwise null.</returns>
    Task<SyncConflict?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a conflict by account ID and file path.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="filePath">The file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The conflict if found, otherwise null.</returns>
    Task<SyncConflict?> GetByFilePathAsync(string accountId, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new conflict.
    /// </summary>
    /// <param name="conflict">The conflict to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(SyncConflict conflict, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing conflict.
    /// </summary>
    /// <param name="conflict">The conflict to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(SyncConflict conflict, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a conflict by its ID.
    /// </summary>
    /// <param name="id">The conflict identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all conflicts for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteByAccountIdAsync(string accountId, CancellationToken cancellationToken = default);
}
