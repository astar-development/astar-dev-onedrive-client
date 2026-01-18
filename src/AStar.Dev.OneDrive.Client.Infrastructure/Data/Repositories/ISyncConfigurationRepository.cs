using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;

/// <summary>
/// Repository for managing sync configuration data.
/// </summary>
public interface ISyncConfigurationRepository
{
    /// <summary>
    /// Gets all sync configurations for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of sync configurations for the account.</returns>
    Task<IReadOnlyList<SyncConfiguration>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all selected folder paths for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of selected folder paths.</returns>
    Task<IReadOnlyList<string>> GetSelectedFoldersAsync(string accountId, CancellationToken cancellationToken = default);

    Task<Result<IList<string>, ErrorResponse>> GetSelectedFolders2Async(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new sync configuration.
    /// </summary>
    /// <param name="configuration">The configuration to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated sync configuration.</returns>
    Task<SyncConfiguration> AddAsync(SyncConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing sync configuration.
    /// </summary>
    /// <param name="configuration">The configuration to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated sync configuration.</returns>
    Task UpdateAsync(SyncConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a sync configuration by its ID.
    /// </summary>
    /// <param name="id">The configuration identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all sync configurations for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteByAccountIdAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves multiple sync configurations for an account in a batch operation.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="configurations">The configurations to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveBatchAsync(string accountId, IEnumerable<SyncConfiguration> configurations, CancellationToken cancellationToken = default);
}
