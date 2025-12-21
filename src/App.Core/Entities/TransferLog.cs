namespace App.Core.Entities;

public sealed record TransferLog(
    string Id,
    TransferType Type,
    string ItemId,
    DateTimeOffset StartedUtc,
    DateTimeOffset? CompletedUtc,
    TransferStatus Status,
    long? BytesTransferred,
    string? Error
);

public enum TransferType { Download, Upload, Delete }
public enum TransferStatus { Pending, InProgress, Success, Failed }
