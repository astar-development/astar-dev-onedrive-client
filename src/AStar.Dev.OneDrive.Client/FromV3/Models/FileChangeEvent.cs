using AStar.Dev.OneDrive.Client.FromV3.Models.Enums;

namespace AStar.Dev.OneDrive.Client.FromV3.Models;

/// <summary>
/// Represents a local file system change event detected by the file watcher.
/// </summary>
/// <param name="AccountId">Account identifier.</param>
/// <param name="LocalPath">Full local file system path.</param>
/// <param name="RelativePath">Path relative to the sync root directory.</param>
/// <param name="ChangeType">Type of file system change.</param>
/// <param name="DetectedUtc">Timestamp when the change was detected.</param>
public sealed record FileChangeEvent(
    string AccountId,
    string LocalPath,
    string RelativePath,
    FileChangeType ChangeType,
    DateTime DetectedUtc
);
