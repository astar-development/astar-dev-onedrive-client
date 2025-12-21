namespace App.Core.Dto;

public sealed record LocalFileInfo(string RelativePath, long Size, DateTimeOffset LastWriteUtc, string? Hash);
