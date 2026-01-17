using AStar.Dev.OneDrive.Client.FromV3.Models;

namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Service for retrieving and managing OneDrive folder hierarchies.
/// </summary>
public interface IFolderTreeService
{
    /// <summary>
    /// Gets the root-level folders for the specified account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Collection of root-level folder nodes.</returns>
    /// <remarks>
    /// Root folders typically include: Documents, Pictures, Music, Videos, and the OneDrive root.
    /// </remarks>
    Task<IReadOnlyList<OneDriveFolderNode>> GetRootFoldersAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the child folders for a specific parent folder.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="parentFolderId">The parent folder's DriveItem ID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Collection of child folder nodes.</returns>
    /// <remarks>
    /// Used for lazy loading tree nodes - only loads children when a node is expanded.
    /// </remarks>
    Task<IReadOnlyList<OneDriveFolderNode>> GetChildFoldersAsync(string accountId, string parentFolderId, bool? parentIsSelected = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the complete folder hierarchy for the specified account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="maxDepth">Maximum depth to traverse (default: unlimited).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Collection of root nodes with fully populated children.</returns>
    /// <remarks>
    /// This method recursively loads the entire folder structure. Use with caution for accounts
    /// with large numbers of folders. Consider using <see cref="GetRootFoldersAsync"/> and
    /// <see cref="GetChildFoldersAsync"/> for lazy loading instead.
    /// </remarks>
    Task<IReadOnlyList<OneDriveFolderNode>> GetFolderHierarchyAsync(string accountId, int? maxDepth = null, CancellationToken cancellationToken = default);
}
