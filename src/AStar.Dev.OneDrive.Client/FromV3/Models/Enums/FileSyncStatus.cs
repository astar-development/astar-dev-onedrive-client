namespace AStar.Dev.OneDrive.Client.FromV3.Models.Enums;

/// <summary>
/// Represents the synchronization status of an individual file.
/// </summary>
public enum FileSyncStatus
{
    /// <summary>
    /// File is in sync between local and OneDrive.
    /// </summary>
    Synced = 0,

    /// <summary>
    /// File is pending upload to OneDrive.
    /// </summary>
    PendingUpload = 1,

    /// <summary>
    /// File is pending download from OneDrive.
    /// </summary>
    PendingDownload = 2,

    /// <summary>
    /// File is currently being uploaded.
    /// </summary>
    Uploading = 3,

    /// <summary>
    /// File is currently being downloaded.
    /// </summary>
    Downloading = 4,

    /// <summary>
    /// File has a conflict that needs resolution.
    /// </summary>
    Conflict = 5,

    /// <summary>
    /// File synchronization failed.
    /// </summary>
    Failed = 6,

    /// <summary>
    /// File has been deleted locally or remotely.
    /// </summary>
    Deleted = 7
}
