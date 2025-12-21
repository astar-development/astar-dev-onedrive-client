namespace App.Core.Dtos;

public sealed record UploadSessionInfo(string UploadUrl, string SessionId, DateTimeOffset ExpiresAt);
