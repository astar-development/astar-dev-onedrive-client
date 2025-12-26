namespace AStar.Dev.OneDrive.Client.Services;

/// <summary>
/// Progress information for sync and transfer operations.
/// </summary>
public sealed record SyncProgress
{
    public required SyncOperationType OperationType { get; init; }
    public int ProcessedFiles { get; init; }
    public int TotalFiles { get; init; }
    public int PendingDownloads { get; init; }
    public int PendingUploads { get; init; }
    public string CurrentOperationMessage { get; init; } = string.Empty;
    public double PercentComplete => TotalFiles > 0 ? ProcessedFiles / (double)TotalFiles * 100 : 0;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    /// <summary>
    /// Total bytes transferred in this operation.
    /// </summary>
    public long BytesTransferred { get; init; }

    /// <summary>
    /// Total bytes to transfer (0 if unknown).
    /// </summary>
    public long TotalBytes { get; init; }

    /// <summary>
    /// Transfer speed in bytes per second (0 if not calculated).
    /// </summary>
    public double BytesPerSecond { get; init; }

    /// <summary>
    /// Transfer speed in megabytes per second.
    /// </summary>
    public double MegabytesPerSecond => BytesPerSecond / (1024.0 * 1024.0);

    /// <summary>
    /// Estimated time remaining for the operation (null if unknown).
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; init; }

    /// <summary>
    /// Duration of the current operation.
    /// </summary>
    public TimeSpan ElapsedTime { get; init; }
}
