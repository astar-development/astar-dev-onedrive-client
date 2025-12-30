using System.Reactive.Subjects;

namespace AStar.Dev.OneDrive.Client.Services;

/// <inheritdoc/>
public class SyncProgressReporter : ISyncProgressReporter
{
    private readonly Subject<SyncProgress> _progressSubject = new();

    public IObservable<SyncProgress> Progress => _progressSubject;

    /// <inheritdoc/>
    public void Report(SyncProgress progress) => _progressSubject.OnNext(progress);

    public static bool ShouldCalculateEta(double bytesPerSecond, long totalBytes, long totalBytesTransferred)
        => bytesPerSecond > 0 && totalBytes > 0 && totalBytesTransferred < totalBytes;
}
