namespace AStar.Dev.OneDrive.Client.Services;

/// <summary>
/// Logs synchronization errors in a consistent manner.
/// </summary>
public interface ISyncErrorLogger
{
    /// <summary>
    /// Logs an error that occurred during sync.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    /// <param name="path">The path or context for the error.</param>
    void LogError(Exception ex, string path);
}
