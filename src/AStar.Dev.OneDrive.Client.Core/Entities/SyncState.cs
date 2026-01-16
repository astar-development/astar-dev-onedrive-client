namespace AStar.Dev.OneDrive.Client.Core.Entities;

public enum SyncState
{
    Unknown,
    PendingDownload,
    Downloaded,
    PendingUpload,
    Uploaded,
    Deleted,
    Error
}
