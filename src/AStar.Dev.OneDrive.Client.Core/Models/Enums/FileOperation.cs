namespace AStar.Dev.OneDrive.Client.Core.Models.Enums;

/// <summary>
/// Represents the type of file operation performed during sync.
/// </summary>
public enum FileOperation
{
    /// <summary>
    /// File was uploaded to OneDrive.
    /// </summary>
    Upload = 0,

    /// <summary>
    /// File was downloaded from OneDrive.
    /// </summary>
    Download = 1,

    /// <summary>
    /// File was deleted locally or from OneDrive.
    /// </summary>
    Delete = 2,

    /// <summary>
    /// Conflict was detected for this file.
    /// </summary>
    ConflictDetected = 3
}
