using AStar.Dev.OneDrive.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Client.SyncConflicts;

/// <summary>
/// Service for resolving sync conflicts between local and remote files.
/// </summary>
public interface IConflictResolver
{
    /// <summary>
    /// Applies the user's chosen resolution strategy to a conflict.
    /// </summary>
    /// <param name="conflict">The conflict to resolve.</param>
    /// <param name="strategy">The resolution strategy chosen by the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ResolveAsync(
        Core.Entities.SyncConflict conflict,
        ConflictResolutionStrategy strategy,
        CancellationToken cancellationToken = default);
}
