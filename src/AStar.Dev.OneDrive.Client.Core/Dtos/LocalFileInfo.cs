namespace AStar.Dev.OneDrive.Client.Core.Dtos;

public sealed record LocalFileInfo(string RelativePath, long Size, DateTimeOffset LastWriteUtc, string? Hash);
