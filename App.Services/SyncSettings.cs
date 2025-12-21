namespace App.Services;

public sealed record SyncSettings(int ParallelDownloads = 4, int BatchSize = 50, int MaxRetries = 3, int RetryBaseDelayMs = 500);

public sealed record SyncStats(int PendingDownloads, int PendingUploads, int ActiveDownloads, int ActiveUploads, int FailedTransfers);
