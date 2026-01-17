using System.Reactive.Linq;
using System.Reactive.Subjects;
using AStar.Dev.OneDrive.Client.FromV3;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Models.Enums;
using AStar.Dev.OneDrive.Client.ViewModels;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.FromV3.ViewModels;

public class SyncProgressViewModelShould
{
    [Fact]
    public void ThrowArgumentExceptionWhenAccountIdIsNull()
    {
        ISyncEngine syncEngine = Substitute.For<ISyncEngine>();
        ILogger<SyncProgressViewModel> logger = Substitute.For<ILogger<SyncProgressViewModel>>();

        Exception? exception = Record.Exception(() => new SyncProgressViewModel(
            null!,
            syncEngine,
            logger));

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentExceptionWhenAccountIdIsEmpty()
    {
        ISyncEngine syncEngine = Substitute.For<ISyncEngine>();
        ILogger<SyncProgressViewModel> logger = Substitute.For<ILogger<SyncProgressViewModel>>();

        Exception? exception = Record.Exception(() => new SyncProgressViewModel(
            string.Empty,
            syncEngine,
            logger));

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<ArgumentException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenSyncEngineIsNull()
    {
        ILogger<SyncProgressViewModel> logger = Substitute.For<ILogger<SyncProgressViewModel>>();

        Exception? exception = Record.Exception(() => new SyncProgressViewModel(
            "test-account",
            null!,
            logger));

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenLoggerIsNull()
    {
        ISyncEngine syncEngine = Substitute.For<ISyncEngine>();

        Exception? exception = Record.Exception(() => new SyncProgressViewModel(
            "test-account",
            syncEngine,
            null!));

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void InitializeWithDefaultValues()
    {
        (SyncProgressViewModel? viewModel, ISyncEngine _, Subject<SyncState> _) = CreateTestViewModel();

        viewModel.CurrentProgress.ShouldBeNull();
        viewModel.IsSyncing.ShouldBeFalse();
        viewModel.StatusMessage.ShouldBe("Ready to sync");
        viewModel.ProgressPercentage.ShouldBe(0);
        viewModel.HasConflicts.ShouldBeFalse();
    }

    [Fact]
    public void UpdateProgressWhenProgressObservableEmits()
    {
        (SyncProgressViewModel? viewModel, ISyncEngine _, Subject<SyncState>? progressSubject) = CreateTestViewModel();

        var progress = new SyncState(
            "test-account",
            SyncStatus.Running,
            10,
            5,
            1000,
            500,
            1,
            1,
            0,
            0,
            1.5,
            null,
            null,
            DateTime.UtcNow
        );

        progressSubject.OnNext(progress);

        viewModel.CurrentProgress.ShouldBe(progress);
        viewModel.ProgressPercentage.ShouldBe(50);
        viewModel.FilesProgressText.ShouldBe("5 of 10 files");
    }

    [Fact]
    public void CalculateProgressPercentageCorrectly()
    {
        (SyncProgressViewModel? viewModel, ISyncEngine _, Subject<SyncState>? progressSubject) = CreateTestViewModel();

        var progress = new SyncState(
            "test-account",
            SyncStatus.Running,
            100,
            25,
            10000,
            2500,
            5,
            5,
            0,
            0,
            2.5,
            null,
            null,
            DateTime.UtcNow
        );

        progressSubject.OnNext(progress);

        viewModel.ProgressPercentage.ShouldBe(25);
    }

    [Fact]
    public void DisplayZeroPercentageWhenTotalFilesIsZero()
    {
        (SyncProgressViewModel? viewModel, ISyncEngine _, Subject<SyncState>? progressSubject) = CreateTestViewModel();

        var progress = new SyncState(
            "test-account",
            SyncStatus.Idle,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            null,
            null,
            DateTime.UtcNow
        );

        progressSubject.OnNext(progress);

        viewModel.ProgressPercentage.ShouldBe(0);
    }

    [Fact]
    public void DisplayTransferDetailsCorrectly()
    {
        (SyncProgressViewModel? viewModel, ISyncEngine _, Subject<SyncState>? progressSubject) = CreateTestViewModel();

        var progress = new SyncState(
            "test-account",
            SyncStatus.Running,
            10,
            8,
            10000,
            8000,
            3,
            2,
            0,
            0,
            1.5,
            null,
            null,
            DateTime.UtcNow
        );

        progressSubject.OnNext(progress);

        viewModel.TransferDetailsText.ShouldContain("uploading");
        viewModel.TransferDetailsText.ShouldContain("downloading");
    }

    [Fact]
    public void DisplayConflictsWhenDetected()
    {
        (SyncProgressViewModel? viewModel, ISyncEngine _, Subject<SyncState>? progressSubject) = CreateTestViewModel();

        var progress = new SyncState(
            "test-account",
            SyncStatus.Running,
            10,
            8,
            10000,
            8000,
            1,
            1,
            0,
            2,
            1.5,
            null,
            null,
            DateTime.UtcNow
        );

        progressSubject.OnNext(progress);

        viewModel.HasConflicts.ShouldBeTrue();
        viewModel.ConflictsText.ShouldBe("⚠ 2 conflict(s) detected");
    }

    [Fact]
    public void NotDisplayConflictsWhenNoneDetected()
    {
        (SyncProgressViewModel? viewModel, ISyncEngine _, Subject<SyncState>? progressSubject) = CreateTestViewModel();

        var progress = new SyncState(
            "test-account",
            SyncStatus.Running,
            10,
            8,
            10000,
            8000,
            1,
            1,
            0,
            0,
            1.5,
            null,
            null,
            DateTime.UtcNow
        );

        progressSubject.OnNext(progress);

        viewModel.HasConflicts.ShouldBeFalse();
        viewModel.ConflictsText.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task StartSyncCommandExecutesSuccessfully()
    {
        (SyncProgressViewModel? viewModel, ISyncEngine? syncEngine, Subject<SyncState> _) = CreateTestViewModel();

        _ = syncEngine.StartSyncAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _ = await viewModel.StartSyncCommand.Execute().FirstAsync();

        await syncEngine.Received(1).StartSyncAsync("test-account", Arg.Any<CancellationToken>());
        viewModel.IsSyncing.ShouldBeFalse();
        viewModel.StatusMessage.ShouldBe("Sync completed successfully");
    }

    [Fact]
    public async Task StartSyncCommandSetsIsSyncingFlag()
    {
        (SyncProgressViewModel? viewModel, ISyncEngine? syncEngine, Subject<SyncState> _) = CreateTestViewModel();
        var isSyncingValues = new List<bool>();

        viewModel.PropertyChanged += (_, e) =>
        {
            if(e.PropertyName == nameof(SyncProgressViewModel.IsSyncing))
            {
                isSyncingValues.Add(viewModel.IsSyncing);
            }
        };

        _ = syncEngine.StartSyncAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async _ => await Task.Delay(50));

        _ = await viewModel.StartSyncCommand.Execute().FirstAsync();

        isSyncingValues.ShouldContain(true);
        viewModel.IsSyncing.ShouldBeFalse();
    }

    [Fact]
    public async Task PauseSyncCommandCallsSyncEngine()
    {
        (SyncProgressViewModel? viewModel, ISyncEngine? syncEngine, Subject<SyncState> _) = CreateTestViewModel();

        viewModel.GetType().GetProperty("IsSyncing")!.SetValue(viewModel, true);

        _ = await viewModel.PauseSyncCommand.Execute().FirstAsync();

        await syncEngine.Received(1).StopSyncAsync();
        viewModel.IsSyncing.ShouldBeFalse();
        viewModel.StatusMessage.ShouldBe("Sync paused");
    }

    [Fact]
    public async Task HandleSyncCancellation()
    {
        (SyncProgressViewModel? viewModel, ISyncEngine? syncEngine, Subject<SyncState> _) = CreateTestViewModel();

        _ = syncEngine.StartSyncAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new OperationCanceledException());

        _ = await viewModel.StartSyncCommand.Execute().FirstAsync();

        viewModel.StatusMessage.ShouldBe("Sync paused");
        viewModel.IsSyncing.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleSyncErrors()
    {
        (SyncProgressViewModel? viewModel, ISyncEngine? syncEngine, Subject<SyncState> _) = CreateTestViewModel();

        _ = syncEngine.StartSyncAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("Test error"));

        _ = await viewModel.StartSyncCommand.Execute().FirstAsync();

        viewModel.StatusMessage.ShouldContain("Sync failed");
        viewModel.IsSyncing.ShouldBeFalse();
    }

    [Fact]
    public void StartSyncCommandCanExecuteWhenNotSyncing()
    {
        (SyncProgressViewModel? viewModel, ISyncEngine _, Subject<SyncState> _) = CreateTestViewModel();

        viewModel.GetType().GetProperty("IsSyncing")!.SetValue(viewModel, false);

        viewModel.StartSyncCommand.CanExecute.FirstAsync().Wait().ShouldBeTrue();
    }

    [Fact]
    public void StartSyncCommandCannotExecuteWhenSyncing()
    {
        (SyncProgressViewModel? viewModel, ISyncEngine _, Subject<SyncState> _) = CreateTestViewModel();

        viewModel.GetType().GetProperty("IsSyncing")!.SetValue(viewModel, true);

        viewModel.StartSyncCommand.CanExecute.FirstAsync().Wait().ShouldBeFalse();
    }

    [Fact]
    public void PauseSyncCommandCanExecuteWhenSyncing()
    {
        (SyncProgressViewModel? viewModel, ISyncEngine _, Subject<SyncState> _) = CreateTestViewModel();

        viewModel.GetType().GetProperty("IsSyncing")!.SetValue(viewModel, true);

        viewModel.PauseSyncCommand.CanExecute.FirstAsync().Wait().ShouldBeTrue();
    }

    [Fact]
    public void PauseSyncCommandCannotExecuteWhenNotSyncing()
    {
        (SyncProgressViewModel? viewModel, ISyncEngine _, Subject<SyncState> _) = CreateTestViewModel();

        viewModel.GetType().GetProperty("IsSyncing")!.SetValue(viewModel, false);

        viewModel.PauseSyncCommand.CanExecute.FirstAsync().Wait().ShouldBeFalse();
    }

    [Fact]
    public void ViewConflictsCommandCanExecuteWhenConflictsExist()
    {
        (SyncProgressViewModel? viewModel, ISyncEngine _, Subject<SyncState>? progressSubject) = CreateTestViewModel();

        var progress = new SyncState(
            "test-account",
            SyncStatus.Running,
            10,
            5,
            10000,
            5000,
            1,
            1,
            0,
            2,
            1.5,
            null,
            null,
            DateTime.UtcNow
        );

        progressSubject.OnNext(progress);

        viewModel.ViewConflictsCommand.CanExecute.FirstAsync().Wait().ShouldBeTrue();
    }

    [Fact]
    public void ViewConflictsCommandCannotExecuteWhenNoConflicts()
    {
        (SyncProgressViewModel? viewModel, ISyncEngine _, Subject<SyncState>? progressSubject) = CreateTestViewModel();

        var progress = new SyncState(
            "test-account",
            SyncStatus.Running,
            10,
            5,
            10000,
            5000,
            1,
            1,
            0,
            0,
            1.5,
            null,
            null,
            DateTime.UtcNow
        );

        progressSubject.OnNext(progress);

        viewModel.ViewConflictsCommand.CanExecute.FirstAsync().Wait().ShouldBeFalse();
    }

    [Fact]
    public void RaisePropertyChangedForAllDerivedProperties()
    {
        (SyncProgressViewModel? viewModel, ISyncEngine _, Subject<SyncState>? progressSubject) = CreateTestViewModel();
        var changedProperties = new List<string>();

        viewModel.PropertyChanged += (_, e) =>
        {
            if(e.PropertyName is not null)
            {
                changedProperties.Add(e.PropertyName);
            }
        };

        var progress = new SyncState(
            "test-account",
            SyncStatus.Running,
            10,
            5,
            10000,
            5000,
            1,
            1,
            0,
            1,
            1.5,
            null,
            null,
            DateTime.UtcNow
        );

        progressSubject.OnNext(progress);

        changedProperties.ShouldContain(nameof(SyncProgressViewModel.CurrentProgress));
        changedProperties.ShouldContain(nameof(SyncProgressViewModel.ProgressPercentage));
        changedProperties.ShouldContain(nameof(SyncProgressViewModel.FilesProgressText));
        changedProperties.ShouldContain(nameof(SyncProgressViewModel.TransferDetailsText));
        changedProperties.ShouldContain(nameof(SyncProgressViewModel.ConflictsText));
        changedProperties.ShouldContain(nameof(SyncProgressViewModel.HasConflicts));
    }

    private static (SyncProgressViewModel ViewModel, ISyncEngine SyncEngine, Subject<SyncState> ProgressSubject)
        CreateTestViewModel()
    {
        ISyncEngine syncEngine = Substitute.For<ISyncEngine>();
        ILogger<SyncProgressViewModel> logger = Substitute.For<ILogger<SyncProgressViewModel>>();

        var progressSubject = new Subject<SyncState>();
        _ = syncEngine.Progress.Returns(progressSubject);

        _ = syncEngine.StartSyncAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _ = syncEngine.StopSyncAsync()
            .Returns(Task.CompletedTask);

        var viewModel = new SyncProgressViewModel("test-account", syncEngine, logger);

        return (viewModel, syncEngine, progressSubject);
    }
}
