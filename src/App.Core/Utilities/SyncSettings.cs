namespace App.Core.Utilities;

public sealed record SyncSettings(int ParallelDownloads = 4, int BatchSize = 50, ConflictPolicy ConflictPolicy = ConflictPolicy.LastWriteWins);

public enum ConflictPolicy { LastWriteWins, KeepLocal, KeepRemote, Prompt }
