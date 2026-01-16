namespace AStar.Dev.OneDrive.Client.Core.Models.Enums;

/// <summary>
/// Types of file system changes that can be detected by the file watcher.
/// </summary>
public enum FileChangeType
{
    /// <summary>
    /// A new file was created.
    /// </summary>
    Created,

    /// <summary>
    /// An existing file was modified.
    /// </summary>
    Modified,

    /// <summary>
    /// A file was deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// A file was renamed.
    /// </summary>
    Renamed
}
