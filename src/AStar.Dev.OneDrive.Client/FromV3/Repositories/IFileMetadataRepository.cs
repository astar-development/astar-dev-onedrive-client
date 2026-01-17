using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Models.Enums;

namespace AStar.Dev.OneDrive.Client.FromV3.Repositories;

/// <summary>
/// Repository for managing file metadata.
/// </summary>
public interface IFileMetadataRepository
{
    /// <summary>
    /// Gets all file metadata for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of file metadata for the account.</returns>
    Task<IReadOnlyList<FileMetadata>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file metadata by its ID.
    /// </summary>
    /// <param name="id">The file identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file metadata if found, otherwise null.</returns>
    Task<FileMetadata?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file metadata by account ID and path.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="path">The file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file metadata if found, otherwise null.</returns>
    Task<FileMetadata?> GetByPathAsync(string accountId, string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all files with a specific sync status for an account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="status">The sync status to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of file metadata matching the status.</returns>
    Task<IReadOnlyList<FileMetadata>> GetByStatusAsync(string accountId, FileSyncStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds new file metadata.
    /// </summary>
    /// <param name="fileMetadata">The file metadata to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(FileMetadata fileMetadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates existing file metadata.
    /// </summary>
    /// <param name="fileMetadata">The file metadata to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(FileMetadata fileMetadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes file metadata by its ID.
    /// </summary>
    /// <param name="id">The file identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all file metadata for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteByAccountIdAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves multiple file metadata entries in a batch operation.
    /// </summary>
    /// <param name="fileMetadataList">The file metadata entries to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveBatchAsync(IEnumerable<FileMetadata> fileMetadataList, CancellationToken cancellationToken = default);
}
