namespace AStar.Dev.OneDrive.Client.FromV3.Models.Enums;

/// <summary>
/// Represents the direction of file synchronization.
/// </summary>
public enum SyncDirection
{
    /// <summary>
    /// File was uploaded to OneDrive.
    /// </summary>
    Upload = 0,

    /// <summary>
    /// File was downloaded from OneDrive.
    /// </summary>
    Download = 1
}
