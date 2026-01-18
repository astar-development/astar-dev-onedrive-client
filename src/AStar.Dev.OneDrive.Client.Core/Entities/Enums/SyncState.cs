namespace AStar.Dev.OneDrive.Client.Core.Entities.Enums;

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
