using AStar.Dev.OneDrive.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Client.Core.Entities;

/// <summary>
/// Represents the current state of synchronization for an account.
/// </summary>
/// <param name="AccountId">Account identifier.</param>
/// <param name="Status">Current synchronization status.</param>
/// <param name="TotalFiles">Total number of files to synchronize.</param>
/// <param name="CompletedFiles">Number of files already synchronized.</param>
/// <param name="TotalBytes">Total bytes to synchronize.</param>
/// <param name="CompletedBytes">Number of bytes already synchronized.</param>
/// <param name="FilesDownloading">Number of files currently downloading.</param>
/// <param name="FilesUploading">Number of files currently uploading.</param>
/// <param name="FilesDeleted">Number of files deleted during sync.</param>
/// <param name="ConflictsDetected">Number of conflicts detected.</param>
/// <param name="MegabytesPerSecond">Current transfer speed in MB/s.</param>
/// <param name="EstimatedSecondsRemaining">Estimated seconds until completion.</param>
/// <param name="CurrentScanningFolder">The folder path currently being scanned (null when not scanning).</param>
/// <param name="LastUpdateUtc">Timestamp of the last state update.</param>
public sealed record SyncState(
    string AccountId,
    SyncStatus Status,
    int TotalFiles,
    int CompletedFiles,
    long TotalBytes,
    long CompletedBytes,
    int FilesDownloading,
    int FilesUploading,
    int FilesDeleted,
    int ConflictsDetected,
    double MegabytesPerSecond,
    int? EstimatedSecondsRemaining,
    string? CurrentScanningFolder,
    DateTime? LastUpdateUtc
)
{
    public static SyncState CreateInitial(string accountId)
        => new(
            AccountId: accountId,
            Status: SyncStatus.Idle,
            TotalFiles: 0,
            CompletedFiles: 0,
            TotalBytes: 0,
            CompletedBytes: 0,
            FilesDownloading: 0,
            FilesUploading: 0,
            FilesDeleted: 0,
            ConflictsDetected: 0,
            MegabytesPerSecond: 0,
            EstimatedSecondsRemaining: null,
            CurrentScanningFolder: null,
            LastUpdateUtc: null);
};
