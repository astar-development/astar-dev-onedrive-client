using AStar.Dev.OneDrive.Client.Core.Entities.Enums;

namespace AStar.Dev.OneDrive.Client.Core.Entities;

public sealed record TransferLog(
    string AccountId,
    string Id,
    TransferType Type,
    string ItemId,
    DateTimeOffset StartedUtc,
    DateTimeOffset? CompletedUtc,
    TransferStatus Status,
    long? BytesTransferred,
    string? Error
);
