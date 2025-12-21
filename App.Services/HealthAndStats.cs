namespace App.Services;

public sealed record TransferProgress(string ItemId, long BytesTransferred, long? TotalBytes, DateTimeOffset Timestamp);
