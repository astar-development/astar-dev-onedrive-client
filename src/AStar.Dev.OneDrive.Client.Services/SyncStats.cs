namespace AStar.Dev.OneDrive.Client.Services;

public sealed record SyncStats(int PendingDownloads, int PendingUploads, int ActiveDownloads, int ActiveUploads, int FailedTransfers);
