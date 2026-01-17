using AStar.Dev.OneDrive.Client.SyncConflicts;

namespace AStar.Dev.OneDrive.Client.FromV3.Entities;

/// <summary>
/// Entity representing a file synchronization conflict in the database.
/// </summary>
public sealed class SyncConflictEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the conflict.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the conflicted file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the local file modification timestamp.
    /// </summary>
    public DateTime LocalModifiedUtc { get; set; }

    /// <summary>
    /// Gets or sets the OneDrive file modification timestamp.
    /// </summary>
    public DateTime RemoteModifiedUtc { get; set; }

    /// <summary>
    /// Gets or sets the local file size in bytes.
    /// </summary>
    public long LocalSize { get; set; }

    /// <summary>
    /// Gets or sets the OneDrive file size in bytes.
    /// </summary>
    public long RemoteSize { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the conflict was detected.
    /// </summary>
    public DateTime DetectedUtc { get; set; }

    /// <summary>
    /// Gets or sets the strategy chosen to resolve the conflict.
    /// </summary>
    public ConflictResolutionStrategy ResolutionStrategy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the conflict has been resolved.
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// Navigation property to the associated account.
    /// </summary>
    public AccountEntity? Account { get; set; }
}
