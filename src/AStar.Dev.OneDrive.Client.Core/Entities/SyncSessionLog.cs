namespace AStar.Dev.OneDrive.Client.Core.Entities;

/// <summary>
/// Database entity for sync session log.
/// </summary>
public class SyncSessionLog
{
    public string Id { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public DateTime StartedUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }
    public int Status { get; set; }
    public int FilesUploaded { get; set; }
    public int FilesDownloaded { get; set; }
    public int FilesDeleted { get; set; }
    public int ConflictsDetected { get; set; }
    public long TotalBytes { get; set; }
}
