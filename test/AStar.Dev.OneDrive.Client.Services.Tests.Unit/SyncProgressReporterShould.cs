using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.Services;
using Xunit;
using Shouldly;

public class SyncProgressReporterShould
{
    [Fact]
    public void Report_EmitsProgressToObservable()
    {
        var reporter = new SyncProgressReporter();
        var progress = new SyncProgress { BytesTransferred = 123, TotalFiles = 2 };
        var received = new List<SyncProgress>();
        using var sub = reporter.Progress.Subscribe(received.Add);
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
        using var sub = reporter.Progress.Subscribe(received.Add);
        received.ShouldBeEmpty();
        reporter.Report(new SyncProgress { BytesTransferred = 1 });
        received.Count.ShouldBe(1);
    }
}
