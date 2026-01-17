namespace AStar.Dev.OneDrive.Client.FromV3.Entities;

/// <summary>
/// Entity for storing sync configuration (folder selections) in the database.
/// </summary>
public sealed class SyncConfigurationEntity
{
    public int Id { get; set; }
    public required string AccountId { get; set; }
    public required string FolderPath { get; set; }
    public bool IsSelected { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}
