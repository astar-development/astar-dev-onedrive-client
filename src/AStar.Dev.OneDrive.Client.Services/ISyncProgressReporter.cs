namespace AStar.Dev.OneDrive.Client.Services;

/// <summary>
/// Reports synchronization progress to observers or UI.
/// </summary>
public interface ISyncProgressReporter
{
    /// <summary>
    /// Reports the current synchronization progress.
    /// </summary>
    /// <param name="progress">The progress information to report.</param>
    void Report(SyncProgress progress);
}
