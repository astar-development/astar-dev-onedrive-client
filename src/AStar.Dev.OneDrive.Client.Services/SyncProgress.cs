namespace AStar.Dev.OneDrive.Client.Models;

public sealed record SyncProgress
{
    public int ProcessedFiles { get; init; }
    public int TotalFiles { get; init; }
    public int PendingDownloads { get; init; }
    public int PendingUploads { get; init; }
    public string CurrentOperation { get; init; } = string.Empty;
    public double PercentComplete => TotalFiles > 0 ? ProcessedFiles / (double)TotalFiles * 100 : 0;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
}
