using AStar.Dev.OneDrive.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Client.Core.Entities;

/// <summary>
/// Entity representing a file synchronization conflict in the database.
/// </summary>
/// <param name="Id">Unique identifier for the conflict.</param>
/// <param name="AccountId">Unique identifier for the account.</param>
/// <param name="FilePath">Path to the conflicted file.</param>
/// <param name="LocalModifiedUtc">Timestamp of the last local modification.</param>
/// <param name="RemoteModifiedUtc">Timestamp of the last remote modification.</param>
/// <param name="LocalSize">Size of the local file.</param>
/// <param name="RemoteSize">Size of the remote file.</param>
/// <param name="DetectedUtc">Timestamp when the conflict was detected.</param>
/// <param name="ResolutionStrategy">Resolution strategy for the conflict.</param>
/// <param name="IsResolved">Indicates whether the conflict has been resolved.</param>
public sealed record SyncConflict(string Id, string AccountId, string FilePath, DateTime LocalModifiedUtc, DateTime RemoteModifiedUtc, long LocalSize,long RemoteSize,DateTime DetectedUtc, ConflictResolutionStrategy ResolutionStrategy = ConflictResolutionStrategy.None, bool IsResolved=false)
{
    /// <summary>
    /// Navigation property to the associated account.
    /// </summary>
    public Account? Account { get; set; }

    public static SyncConflict CreateUnresolvedConflict(string accountId, string filePath, DateTime localModifiedUtc, DateTime remoteModifiedUtc, long localSize, long remoteSize) => new
    (       Guid.CreateVersion7().ToString(),
         accountId,
         filePath,
         localModifiedUtc,
         remoteModifiedUtc,
         localSize,
         remoteSize,
         DateTime.UtcNow,
         ConflictResolutionStrategy.None,
         false
    );
}

