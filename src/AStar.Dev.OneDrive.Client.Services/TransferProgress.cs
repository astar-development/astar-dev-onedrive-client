namespace AStar.Dev.OneDrive.Client.Services;

public sealed record TransferProgress(string ItemId, long BytesTransferred, long? TotalBytes, DateTimeOffset Timestamp);
