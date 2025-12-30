using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.OneDrive.Client.Core.Dtos;
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
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <param name="progressCallback">Callback to report progress.</param>
    /// <returns>Tuple with final delta, page count, and total items processed.</returns>
    Task<(string? finalDelta, int pageCount, int totalItemsProcessed)> ProcessAllDeltaPagesAsync(CancellationToken cancellationToken, Action<SyncProgress>? progressCallback);
}
