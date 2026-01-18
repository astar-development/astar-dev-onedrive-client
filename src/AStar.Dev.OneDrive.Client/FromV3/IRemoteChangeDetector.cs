using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Service for detecting changes on OneDrive using delta queries.
/// </summary>
public interface IRemoteChangeDetector
{
    /// <summary>
    /// Detects changes on OneDrive for the specified account and folder.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="folderPath">The OneDrive folder path to monitor.</param>
    /// <param name="previousDeltaLink">Previous delta link for incremental sync, or null for initial sync.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple containing list of changed file metadata and new delta link for next sync.</returns>
    Task<(IReadOnlyList<FileMetadata> Changes, string? NewDeltaLink)> DetectChangesAsync(
        string accountId,
        string folderPath,
        string? previousDeltaLink,
        CancellationToken cancellationToken = default);
}
