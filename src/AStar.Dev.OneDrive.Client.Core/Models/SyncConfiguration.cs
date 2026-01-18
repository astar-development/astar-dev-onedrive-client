namespace AStar.Dev.OneDrive.Client.Core.Models;

/// <summary>
/// Represents folder selection configuration for synchronization.
/// </summary>
/// <param name="Id">Unique identifier for this configuration entry.</param>
/// <param name="AccountId">Account identifier this configuration belongs to.</param>
/// <param name="FolderPath">OneDrive folder path.</param>
/// <param name="IsSelected">Indicates whether this folder is selected for synchronization.</param>
/// <param name="LastModifiedUtc">Timestamp when this configuration was last modified.</param>
public sealed record SyncConfiguration(
    int Id,
    string AccountId,
    string FolderPath,
    bool IsSelected,
    DateTime LastModifiedUtc
);
