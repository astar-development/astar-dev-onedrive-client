namespace AStar.Dev.OneDrive.Client.Services;

/// <summary>
/// Settings related to synchronization behavior.
/// This class needs to be mutable for easy updates during runtime as it is injected.
/// </summary>
public sealed class SyncSettings()
{
    public int MaxParallelDownloads { get; set; } = 3;
    public int DownloadBatchSize { get; set; } = 100;
    public int MaxRetries { get; set; } = 2;
    public int RetryBaseDelayMs { get; set; } = 400;
}
