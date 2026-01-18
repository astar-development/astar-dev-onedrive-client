using AStar.Dev.OneDrive.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Client.Core.Entities;

/// <summary>
/// Represents a summary of a sync session.
/// </summary>
/// <param name="Id">Unique identifier for the sync session.</param>
/// <param name="AccountId">The account identifier.</param>
/// <param name="StartedUtc">When the sync started.</param>
/// <param name="CompletedUtc">When the sync completed (null if still running).</param>
/// <param name="Status">Final status of the sync.</param>
/// <param name="FilesUploaded">Number of files uploaded.</param>
/// <param name="FilesDownloaded">Number of files downloaded.</param>
/// <param name="FilesDeleted">Number of files deleted.</param>
/// <param name="ConflictsDetected">Number of conflicts detected.</param>
/// <param name="TotalBytes">Total bytes transferred.</param>
public record SyncSessionLog(
    string Id,
    string AccountId,
    DateTime StartedUtc,
    DateTime? CompletedUtc,
    SyncStatus Status,
    int FilesUploaded,
    int FilesDownloaded,
    int FilesDeleted,
    int ConflictsDetected,
    long TotalBytes)
{
    public static SyncSessionLog CreateInitialRunning(string accountId) => new(
            Id: Guid.CreateVersion7().ToString(),
            AccountId: accountId,
            StartedUtc: DateTime.UtcNow,
            CompletedUtc: null,
            Status: SyncStatus.Running,
            FilesUploaded: 0,
            FilesDownloaded: 0,
            FilesDeleted: 0,
            ConflictsDetected: 0,
            TotalBytes: 0L);
}
