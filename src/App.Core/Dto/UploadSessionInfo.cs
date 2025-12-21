namespace App.Core.Dto;

public sealed record UploadSessionInfo(string UploadUrl, string SessionId, DateTimeOffset ExpiresAt);
