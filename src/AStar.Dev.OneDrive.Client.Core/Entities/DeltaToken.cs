namespace AStar.Dev.OneDrive.Client.Core.Entities;

public sealed record DeltaToken(string Id, string Token, DateTimeOffset LastSyncedUtc);
