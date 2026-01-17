namespace AStar.Dev.OneDrive.Client.FromV3.Models;

/// <summary>
/// Represents account information for a OneDrive account.
/// </summary>
/// <param name="AccountId">Unique identifier for the account.</param>
/// <param name="DisplayName">Display name of the account holder.</param>
/// <param name="LocalSyncPath">Local directory path for synchronization.</param>
/// <param name="IsAuthenticated">Indicates whether the account is currently authenticated.</param>
/// <param name="LastSyncUtc">Timestamp of the last successful synchronization.</param>
/// <param name="DeltaToken">Delta token for incremental synchronization.</param>
/// <param name="EnableDetailedSyncLogging">Enables detailed logging of all file operations during sync.</param>
/// <param name="EnableDebugLogging">Enables debug logging to database for historical review.</param>
/// <param name="MaxParallelUpDownloads">Maximum number of parallel upload/download operations (1-10).</param>
/// <param name="MaxItemsInBatch">Maximum number of items to process in a single batch (1-100).</param>
/// <param name="AutoSyncIntervalMinutes">Interval in minutes for automatic remote sync checks (null = disabled, 60-1440).</param>
public sealed record AccountInfo(
    string AccountId,
    string DisplayName,
    string LocalSyncPath,
    bool IsAuthenticated,
    DateTime? LastSyncUtc,
    string? DeltaToken,
    bool EnableDetailedSyncLogging,
    bool EnableDebugLogging,
    int MaxParallelUpDownloads,
    int MaxItemsInBatch,
    int? AutoSyncIntervalMinutes
);
