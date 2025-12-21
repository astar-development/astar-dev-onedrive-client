namespace App.Core.Dtos;

public sealed record LocalFileInfo(string RelativePath, long Size, DateTimeOffset LastWriteUtc, string? Hash);
