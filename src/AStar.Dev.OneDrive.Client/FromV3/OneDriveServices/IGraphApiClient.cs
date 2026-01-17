using Microsoft.Graph.Models;

namespace AStar.Dev.OneDrive.Client.FromV3.OneDriveServices;

/// <summary>
/// Wrapper interface for Microsoft Graph API client to enable testing.
/// </summary>
/// <remarks>
/// Microsoft.Graph.GraphServiceClient and related classes are difficult to mock directly.
/// This interface provides a testable abstraction over OneDrive folder and file operations.
/// All methods require an accountId to specify which account to use for the operation.
/// </remarks>
public interface IGraphApiClient
{
    /// <summary>
    /// Gets the root drive for the specified account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The user's root drive.</returns>
    Task<Drive?> GetMyDriveAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the root folder of the user's drive for the specified account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The root drive item.</returns>
    Task<DriveItem?> GetDriveRootAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the children (files and folders) of a specific drive item.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="itemId">The drive item ID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Collection of child drive items.</returns>
    Task<IEnumerable<DriveItem>> GetDriveItemChildrenAsync(string accountId, string itemId, CancellationToken cancellationToken = default);
    Task<IEnumerable<DriveItem>> GetDriveItemChildrenAsync(string accountId, string itemId, int maxItemsInBatch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific drive item by its ID.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="itemId">The drive item ID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The drive item, or null if not found.</returns>
    Task<DriveItem?> GetDriveItemAsync(string accountId, string itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the children of the root folder.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Collection of root-level drive items.</returns>
    Task<IEnumerable<DriveItem>> GetRootChildrenAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from OneDrive to a local path.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="itemId">The drive item ID of the file to download.</param>
    /// <param name="localFilePath">The local file path where the file should be saved.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous download operation.</returns>
    Task DownloadFileAsync(string accountId, string itemId, string localFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a file to OneDrive at the specified path.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="localFilePath">The local file path to upload.</param>
    /// <param name="remotePath">The remote path where the file should be uploaded (relative to drive root, e.g., "/Documents/file.txt").</param>
    /// <param name="progress">Optional progress reporter for upload progress (reports bytes uploaded).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The uploaded DriveItem with OneDrive metadata (ID, CTag, ETag, etc.).</returns>
    /// <remarks>
    /// Uses simple upload for files under 4MB and resumable upload session for larger files.
    /// The remotePath should include the filename and be relative to the drive root.
    /// </remarks>
    Task<DriveItem> UploadFileAsync(string accountId, string localFilePath, string remotePath, IProgress<long>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from OneDrive.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="itemId">The OneDrive item ID to delete.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    /// <remarks>
    /// This permanently deletes the file from OneDrive (moves to recycle bin if available).
    /// </remarks>
    Task DeleteFileAsync(string accountId, string itemId, CancellationToken cancellationToken = default);
}
