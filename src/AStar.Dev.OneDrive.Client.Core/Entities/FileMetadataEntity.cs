namespace AStar.Dev.OneDrive.Client.Core.Entities;

/// <summary>
/// Entity for tracking synced files and their metadata in the database.
/// </summary>
public sealed class FileMetadataEntity
{
    public required string Id { get; set; }
    public required string AccountId { get; set; }
    public required string Name { get; set; }
    public required string Path { get; set; }
    public long Size { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public required string LocalPath { get; set; }
    public string? CTag { get; set; }
    public string? ETag { get; set; }
    public string? LocalHash { get; set; }
    public int SyncStatus { get; set; }
    public int? LastSyncDirection { get; set; }
}
