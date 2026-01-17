namespace AStar.Dev.OneDrive.Client.FromV3.Models;

/// <summary>
/// Represents a debug log entry.
/// </summary>
/// <param name="Id">Unique identifier for the log entry.</param>
/// <param name="AccountId">Account ID associated with the log entry.</param>
/// <param name="Timestamp">When the log entry was created.</param>
/// <param name="LogLevel">Severity level (Info, Error, Entry, Exit).</param>
/// <param name="Source">Source of the log (typically ClassName.MethodName).</param>
/// <param name="Message">Log message content.</param>
/// <param name="Exception">Exception details if applicable.</param>
public sealed record DebugLogEntry(
    int Id,
    string AccountId,
    DateTime Timestamp,
    string LogLevel,
    string Source,
    string Message,
    string? Exception
);
