using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Services;

public static class SyncExtensions
{
    public static void LogSyncError(this ILogger logger, Exception ex, string path)
        => logger.LogError(ex, "Error processing download item {Path}", path);

    public static void ReportSyncProgress(this ISubject<SyncProgress> progressSubject, SyncProgress progress)
        => progressSubject.OnNext(progress);
}
