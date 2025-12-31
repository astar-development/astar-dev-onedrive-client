using System.Reactive.Linq;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit;

public class SyncProgressReporterShould
{
    [Fact]
    public void Report_EmitsProgressToObservable()
    {
        var reporter = new SyncProgressReporter();
        var progress = new SyncProgress { BytesTransferred = 123, TotalFiles = 2, OperationType = SyncOperationType.Syncing };
        var received = new List<SyncProgress>();
        using IDisposable sub = reporter.Progress.Subscribe(received.Add);
        reporter.Report(progress);
        received.Count.ShouldBe(1);
        received[0].BytesTransferred.ShouldBe(123);
        received[0].TotalFiles.ShouldBe(2);
    }

    [Fact]
    public void Progress_IsColdObservableUntilReportCalled()
    {
        var reporter = new SyncProgressReporter();
        var received = new List<SyncProgress>();
        using IDisposable sub = reporter.Progress.Subscribe(received.Add);
        received.ShouldBeEmpty();
        reporter.Report(new SyncProgress { BytesTransferred = 1, OperationType = SyncOperationType.Syncing });
        received.Count.ShouldBe(1);
    }
}
