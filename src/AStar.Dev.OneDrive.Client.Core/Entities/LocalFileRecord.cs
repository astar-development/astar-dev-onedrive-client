namespace AStar.Dev.OneDrive.Client.Core.Entities;

public sealed record LocalFileRecord(
    string Id,
    string RelativePath,
    string? Hash,
    long Size,
    DateTimeOffset LastWriteUtc,
    SyncState SyncState
);

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
