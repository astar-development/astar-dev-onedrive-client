namespace AStar.Dev.OneDrive.Client.Core.Models.Enums;

/// <summary>
/// Represents the current synchronization status.
/// </summary>
public enum SyncStatus
{
    /// <summary>
    /// Synchronization is idle and not running.
    /// </summary>
    Idle = 0,

    /// <summary>
    /// Synchronization is currently in progress.
    /// </summary>
    Running = 1,

    /// <summary>
    /// Synchronization has been paused by the user.
    /// </summary>
    Paused = 2,

    /// <summary>
    /// Synchronization completed successfully.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Synchronization failed with errors.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Synchronization is waiting to start.
    /// </summary>
    Queued = 5
}
