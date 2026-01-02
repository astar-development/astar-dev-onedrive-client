using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Services;

/// <summary>
/// Processes delta pages from the OneDrive Graph API and applies them to the local repository.
/// </summary>
public interface IDeltaPageProcessor
{
    /// <summary>
    /// Processes all delta pages, applies them to the repository, and returns the final delta link, page count, and total items processed.
    /// </summary>
    Task<(string? finalDelta, int pageCount, int totalItemsProcessed)> ProcessAllDeltaPagesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Processes all delta pages and reports progress via callback.
    /// </summary>
    /// <param name="deltaToken">The delta token to start from.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <param name="progressCallback">Callback to report progress.</param>
    /// <returns>Tuple with final delta, page count, and total items processed.</returns>
    Task<(DeltaToken finalDelta, int pageCount, int totalItemsProcessed)> ProcessAllDeltaPagesAsync(DeltaToken deltaToken, CancellationToken cancellationToken, Action<SyncProgress>? progressCallback);
}
