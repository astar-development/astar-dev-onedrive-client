namespace AStar.Dev.OneDrive.Client.Core.Models.Enums;

/// <summary>
/// Represents the strategy for resolving file synchronization conflicts.
/// </summary>
public enum ConflictResolutionStrategy
{
    /// <summary>
    /// No action taken; conflict remains unresolved.
    /// </summary>
    None = 0,

    /// <summary>
    /// Keep the local version and upload to OneDrive.
    /// </summary>
    KeepLocal = 1,

    /// <summary>
    /// Keep the OneDrive version and download to local.
    /// </summary>
    KeepRemote = 2,

    /// <summary>
    /// Keep both versions with different file names.
    /// </summary>
    KeepBoth = 3,

    /// <summary>
    /// Keep the newer version based on modification time.
    /// </summary>
    KeepNewer = 4
}
