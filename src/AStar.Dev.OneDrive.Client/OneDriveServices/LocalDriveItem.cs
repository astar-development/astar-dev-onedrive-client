using AStar.Dev.OneDrive.Client.Data;

namespace AStar.Dev.OneDrive.Client.OneDriveServices;

/// <summary>
///     Represents a local drive item, which could be a folder or file,
///     with metadata for synchronization with a OneDrive-like storage system.
/// </summary>
public class LocalDriveItem
{
    public LocalDriveId Id { get; set; } = LocalDriveId.Empty;
    public string PathId { get; set; } = ""; // Path-based ID for mirroring
    public string? Name { get; set; }
    public bool IsFolder { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public string? ParentPath { get; set; }
    public string? ETag { get; set; }
}
