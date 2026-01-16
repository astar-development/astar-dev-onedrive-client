namespace AStar.Dev.OneDrive.Client.Core.Entities;

/// <summary>
/// Database entity for file operation log.
/// </summary>
public class FileOperationLog
{
    public string Id { get; set; } = string.Empty;
    public string SyncSessionId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int Operation { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public string? OneDriveId { get; set; }
    public long FileSize { get; set; }
    public string? LocalHash { get; set; }
    public string? RemoteHash { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
}
