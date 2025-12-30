using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Services;

/// <inheritdoc/>
public class SyncErrorLogger(ILogger logger) : ISyncErrorLogger
{
    /// <inheritdoc/>
    public void LogError(Exception ex, string path) => logger.LogError(ex, "Error processing download item {Path}", path);
}
