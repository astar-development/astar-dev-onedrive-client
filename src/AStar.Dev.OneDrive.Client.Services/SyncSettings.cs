namespace AStar.Dev.OneDrive.Client.Services;

public sealed record SyncSettings(int ParallelDownloads = 4, int BatchSize = 50, int MaxRetries = 3, int RetryBaseDelayMs = 500);
