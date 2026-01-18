namespace AStar.Dev.OneDrive.Client.Core.Entities;

/// <summary>
/// Entity for storing sync configuration (folder selections) in the database.
/// </summary>
public sealed class SyncConfiguration(int id, string accountId, string folderPath, bool isSelected, DateTime lastModifiedUtc)
{
    public int Id { get; set; } = id;
    public string AccountId { get; set; } = accountId;
    public string FolderPath { get; set; } = folderPath;
    public bool IsSelected { get; set; } = isSelected;
    public DateTime LastModifiedUtc { get; set; } = lastModifiedUtc;
}
