namespace App.Core.Entities;

public sealed record DeltaToken(string Id, string Token, DateTimeOffset LastSyncedUtc);
