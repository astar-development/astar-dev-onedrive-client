namespace AStar.Dev.OneDrive.Client.Core.Entities;

/// <summary>
/// Entity for storing sync configuration (folder selections) in the database.
/// </summary>
public sealed class SyncConfiguration
{
    public int Id { get; set; }
    public required string AccountId { get; set; }
    public required string FolderPath { get; set; }
    public bool IsSelected { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}
