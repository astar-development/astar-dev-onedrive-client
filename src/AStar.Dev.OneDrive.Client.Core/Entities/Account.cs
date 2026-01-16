namespace AStar.Dev.OneDrive.Client.Core.Entities;

/// <summary>
/// Entity for storing account information in the database.
/// </summary>
public sealed class Account
{
    public required string AccountId { get; set; }
    public required string DisplayName { get; set; }
    public required string LocalSyncPath { get; set; }
    public bool IsAuthenticated { get; set; }
    public DateTime? LastSyncUtc { get; set; }
    public string? DeltaToken { get; set; }
    public bool EnableDetailedSyncLogging { get; set; }
    public bool EnableDebugLogging { get; set; }
    public int MaxParallelUpDownloads { get; set; }
    public int MaxItemsInBatch { get; set; }
    public int? AutoSyncIntervalMinutes { get; set; }
}
