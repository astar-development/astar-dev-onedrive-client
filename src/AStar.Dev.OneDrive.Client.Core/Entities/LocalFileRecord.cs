namespace AStar.Dev.OneDrive.Client.Core.Entities;

public sealed record LocalFileRecord(
    string AccountId,
    string Id,
    string RelativePath,
    string? Hash,
    long Size,
    DateTimeOffset LastWriteUtc,
    AStar.Dev.OneDrive.Client.Core.Entities.Enums.SyncState SyncState
);
