using System.Reactive.Subjects;
using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.FromV3;

public static class ProgressReporterService
{
    public static (int placeholder, BehaviorSubject<SyncState> progressSubject) ReportProgress(SyncState progress, BehaviorSubject<SyncState> progressSubject, DateTime lastProgressUpdate, long lastCompletedBytes, List<(DateTime Timestamp, long Bytes)> transferHistory)
    {
        DateTime now = DateTime.UtcNow;
        var elapsedSeconds = (now - lastProgressUpdate).TotalSeconds;

        double megabytesPerSecond = 0;
        if(elapsedSeconds > 0.1)
        {
            var bytesDelta = progress.CompletedBytes - lastCompletedBytes;
            if(bytesDelta > 0)
            {
                var megabytesDelta = bytesDelta / (1024.0 * 1024.0);
                megabytesPerSecond = megabytesDelta / elapsedSeconds;

                transferHistory.Add((now, progress.CompletedBytes));
                if(transferHistory.Count > 10) transferHistory.RemoveAt(0);

                if(transferHistory.Count >= 2)
                {
                    var totalElapsed = (now - transferHistory[0].Timestamp).TotalSeconds;
                    var totalTransferred = progress.CompletedBytes - transferHistory[0].Bytes;
                    if(totalElapsed > 0) megabytesPerSecond = totalTransferred / (1024.0 * 1024.0) / totalElapsed;
                }
            }
        }

        int? estimatedSecondsRemaining = null;
        var bytesForEta = progress.TotalBytes;
        if(megabytesPerSecond > 0.01 && progress.CompletedBytes < bytesForEta)
        {
            var remainingBytes = bytesForEta - progress.CompletedBytes;
            var remainingMegabytes = remainingBytes / (1024.0 * 1024.0);
            estimatedSecondsRemaining = (int)Math.Ceiling(remainingMegabytes / megabytesPerSecond);
        }

        SyncState updatedProgress = progress with
        {
            MegabytesPerSecond = megabytesPerSecond,
            EstimatedSecondsRemaining = estimatedSecondsRemaining,
            LastUpdateUtc = now
        };
        progressSubject.OnNext(updatedProgress);

        return (0, progressSubject);
    }
}
