namespace AStar.Dev.OneDrive.Client.Core.Entities;

public sealed record DriveItemRecord(
    string Id,
    string DriveItemId,
    string RelativePath,
    string? ETag,
    string? CTag,
    long Size,
    DateTimeOffset LastModifiedUtc,
    bool IsFolder,
    bool IsDeleted
);
