using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;

/// <summary>
/// Repository for managing file operation logs.
/// </summary>
public interface IFileOperationLogRepository
{
    /// <summary>
    /// Gets all file operations for a sync session.
    /// </summary>
    /// <param name="syncSessionId">The sync session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of file operations.</returns>
    Task<IReadOnlyList<FileOperationLog>> GetBySessionIdAsync(string syncSessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all file operations for an account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of file operations.</returns>
    Task<IReadOnlyList<FileOperationLog>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file operations for an account with paging support.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="pageSize">Number of records to return.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of file operations.</returns>
    Task<IReadOnlyList<FileOperationLog>> GetByAccountIdAsync(string accountId, int pageSize, int skip, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new file operation log.
    /// </summary>
    /// <param name="operationLog">The operation log to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(FileOperationLog operationLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old file operation logs for an account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="olderThan">Delete operations older than this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteOldOperationsAsync(string accountId, DateTime olderThan, CancellationToken cancellationToken = default);
}
