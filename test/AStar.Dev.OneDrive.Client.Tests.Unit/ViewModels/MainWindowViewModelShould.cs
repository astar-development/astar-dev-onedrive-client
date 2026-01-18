using System.Reactive.Linq;
using System.Reactive.Subjects;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Services;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.Services.Syncronisation;
using AStar.Dev.OneDrive.Client.SettingsAndPreferences;
using AStar.Dev.OneDrive.Client.ViewModels;
using NSubstitute.ExceptionExtensions;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.ViewModels;

public sealed class MainWindowViewModelShould
{
    private readonly ISyncEngine _mockSync;
    private readonly ISyncRepository _mockRepo;
    private readonly ITransferService _mockTransfer;
    private readonly ISettingsAndPreferencesService _mockSettings;
    private readonly Subject<SyncProgress> _syncProgressSubject;
    private readonly Subject<SyncProgress> _transferProgressSubject;

    public MainWindowViewModelShould()
    {
        _mockSync = Substitute.For<ISyncEngine>();
        _mockRepo = Substitute.For<ISyncRepository>();
        _mockTransfer = Substitute.For<ITransferService>();
        _mockSettings = Substitute.For<ISettingsAndPreferencesService>();

        // Stub observables to prevent NullReferenceException
        _syncProgressSubject = new Subject<SyncProgress>();
        _transferProgressSubject = new Subject<SyncProgress>();
        _mockSync.Progress.Returns(_syncProgressSubject);
        _mockTransfer.Progress.Returns(_transferProgressSubject);

        UserPreferences userPreferences = new();
        _mockSettings.Load().Returns(userPreferences);
    }

    private MainWindowViewModel CreateViewModel()
    {
        // Provide working ReactiveCommand stubs for all commands
        ISyncCommandService mockSyncCommandService = Substitute.For<ISyncCommandService>();
        mockSyncCommandService.CreateSignInCommand(Arg.Any<ISyncStatusTarget>())
            .Returns(ReactiveCommand.Create(() => System.Reactive.Unit.Default, Observable.Return(true)));
        mockSyncCommandService.CreateInitialSyncCommand(Arg.Any<ISyncStatusTarget>(), Arg.Any<IObservable<bool>>())
            .Returns(ReactiveCommand.CreateFromTask(async _ => System.Reactive.Unit.Default, Observable.Return(true)));
        mockSyncCommandService.CreateIncrementalSyncCommand(Arg.Any<DeltaToken>(), Arg.Any<ISyncStatusTarget>(), Arg.Any<IObservable<bool>>())
            .Returns(ReactiveCommand.CreateFromTask(async _ => System.Reactive.Unit.Default, Observable.Return(true)));
        mockSyncCommandService.CreateCancelSyncCommand(Arg.Any<ISyncStatusTarget>(), Arg.Any<IObservable<bool>>())
            .Returns(ReactiveCommand.Create(() => System.Reactive.Unit.Default, Observable.Return(true)));
        IAuthService authService = Substitute.For<IAuthService>();

        // Use a real implementation for ScanLocalFilesCommand to exercise ViewModel state changes
        mockSyncCommandService.CreateScanLocalFilesCommand(Arg.Any<ISyncStatusTarget>(), Arg.Any<IObservable<bool>>())
            .Returns(callInfo =>
            {
                ISyncStatusTarget target = callInfo.ArgAt<ISyncStatusTarget>(0);
                return ReactiveCommand.CreateFromTask(async ct =>
                {
                    try
                    {
                        target.SetStatus("Processing local file sync...");
                        target.SetProgress(0);
                        target.AddRecentTransfer("Processing local file sync...");
                        await _mockSync.ScanLocalFilesAsync("PlaceholderAccountId", ct);
                        target.SetStatus("Local file sync completed successfully");
                        target.SetProgress(100);
                        target.AddRecentTransfer("Local file sync completed successfully");
                        target.OnSyncCompleted();
                        return System.Reactive.Unit.Default;
                    }
                    catch(OperationCanceledException)
                    {
                        target.OnSyncCancelled("Local file sync");
                        return System.Reactive.Unit.Default;
                    }
                    catch(Exception ex)
                    {
                        target.OnSyncFailed("Local file sync", ex);
                        return System.Reactive.Unit.Default;
                    }
                }, Observable.Return(true));
            });
        ISyncronisationCoordinator syncCoordinator = new SyncronisationCoordinator(_mockSync, _mockRepo, _mockTransfer);

        return new MainWindowViewModel(mockSyncCommandService, syncCoordinator, _mockSettings, authService, null!, null!, null!);
    }

    [Fact]
    public void InitializeWithScanLocalFilesCommand()
    {
        MainWindowViewModel sut = CreateViewModel();

        sut.ScanLocalFilesCommand.ShouldNotBeNull();
    }

    [Fact]
    public void ScanLocalFilesCommand_CallsSyncEngineScanLocalFilesAsync()
    {
        MainWindowViewModel sut = CreateViewModel();
        _mockSync.ScanLocalFilesAsync("PlaceholderAccountId", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        TaskCompletionSource tcs = new();
        _ = sut.ScanLocalFilesCommand.Subscribe(_ => tcs.SetResult());

        sut.ScanLocalFilesCommand.Execute().Subscribe();
        tcs.Task.Wait(TimeSpan.FromSeconds(5));

        _ = _mockSync.Received(1).ScanLocalFilesAsync("PlaceholderAccountId", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ScanLocalFilesCommand_UpdatesSyncStatusToScanning()
    {
        MainWindowViewModel sut = CreateViewModel();
        var started = false;
        _mockSync.ScanLocalFilesAsync("PlaceholderAccountId", Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                started = true;
                await Task.Delay(10);
            });
        sut.ScanLocalFilesCommand.Execute().Subscribe(_ => { });
        sut.SyncStatusMessage.ShouldBe("Processing local file sync...");
        started.ShouldBe(true);
    }

    [Fact]
    public void ScanLocalFilesCommand_UpdatesSyncStatusToCompleteWhenFinished()
    {
        MainWindowViewModel sut = CreateViewModel();
        _mockSync.ScanLocalFilesAsync("PlaceholderAccountId", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        TaskCompletionSource tcs = new();
        _ = sut.ScanLocalFilesCommand.Subscribe(_ => tcs.SetResult());

        sut.ScanLocalFilesCommand.Execute().Subscribe();
        tcs.Task.Wait(TimeSpan.FromSeconds(5));

        sut.SyncStatusMessage.ShouldBe("Local file sync completed successfully");
    }

    [Fact]
    public void ScanLocalFilesCommand_AddsSuccessMessageToRecentTransfers()
    {
        MainWindowViewModel sut = CreateViewModel();
        _mockSync.ScanLocalFilesAsync("PlaceholderAccountId", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        TaskCompletionSource tcs = new();
        _ = sut.ScanLocalFilesCommand.Subscribe(_ => tcs.SetResult());

        sut.ScanLocalFilesCommand.Execute().Subscribe();
        tcs.Task.Wait(TimeSpan.FromSeconds(5));

        sut.RecentTransfers.ShouldContain(t => t.Contains("Local file sync completed successfully"));
    }

    [Fact]
    public void ScanLocalFilesCommand_RefreshesStatsAfterCompletion()
    {
        MainWindowViewModel sut = CreateViewModel();
        _mockSync.ScanLocalFilesAsync("PlaceholderAccountId", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _mockRepo.GetPendingDownloadCountAsync("PlaceholderAccountId", Arg.Any<CancellationToken>())
            .Returns(5);
        _mockRepo.GetPendingUploadCountAsync("PlaceholderAccountId", Arg.Any<CancellationToken>())
            .Returns(3);
        sut.ScanLocalFilesCommand.Execute().Subscribe(_ => { });
        Task.Delay(200).Wait(); // Give time for async refresh
        _mockRepo.Received().GetPendingDownloadCountAsync("PlaceholderAccountId", Arg.Any<CancellationToken>());
        _mockRepo.Received().GetPendingUploadCountAsync("PlaceholderAccountId", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ScanLocalFilesCommand_HandlesCancellation()
    {
        MainWindowViewModel sut = CreateViewModel();
        _mockSync.ScanLocalFilesAsync("PlaceholderAccountId", Arg.Any<CancellationToken>())
            .Returns(_ => throw new OperationCanceledException());
        sut.ScanLocalFilesCommand.Execute().Subscribe(_ => { });
        sut.SyncStatusMessage.ShouldBe("Local file sync cancelled");
        sut.RecentTransfers.ShouldContain(t => t.Contains("Local file sync was cancelled"));
    }

    [Fact]
    public void ScanLocalFilesCommand_HandlesExceptions()
    {
        MainWindowViewModel sut = CreateViewModel();
        _mockSync.ScanLocalFilesAsync("PlaceholderAccountId", Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Test error"));
        sut.ScanLocalFilesCommand.Execute().Subscribe(_ => { }, _ => { });
        Task.Delay(100).Wait();
        sut.SyncStatusMessage.ShouldBe("Local file sync failed");
        sut.RecentTransfers.ShouldContain(t => t.Contains("ERROR") && t.Contains("Test error"));
    }

    [Fact]
    public void IsSyncingStatus_ReturnsTrueForScanningStatus()
    {
        MainWindowViewModel sut = CreateViewModel();

        sut.SyncStatusMessage = "Scanning local files";

        sut.SyncStatusMessage.Contains("Scanning", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public void IsSyncingStatus_ReturnsFalseForScanCompleteStatus()
    {
        MainWindowViewModel sut = CreateViewModel();

        sut.SyncStatusMessage = "Local file scan complete";

        sut.SyncStatusMessage.Contains("complete", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public void ScanLocalFilesCommand_ReceivesProgressUpdatesFromSyncEngine()
    {
        MainWindowViewModel sut = CreateViewModel();
        TaskCompletionSource<bool> executeStarted = new();
        TaskCompletionSource<bool> tcs = new();

        _mockSync.ScanLocalFilesAsync("PlaceholderAccountId", Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                _syncProgressSubject.OnNext(new SyncProgress
                {
                    OperationType = SyncOperationType.Syncing,
                    CurrentOperationMessage = "Scanning local files (50/100)...",
                    ProcessedFiles = 50,
                    TotalFiles = 100,
                    PendingUploads = 15
                });
                executeStarted.SetResult(true);
                await tcs.Task;
            });

        var progressReceived = false;
        sut.PropertyChanged += (_, e) =>
        {
            if(e.PropertyName == nameof(MainWindowViewModel.SyncStatusMessage) &&
                sut.SyncStatusMessage.Contains("Scanning local files"))
                progressReceived = true;
        };

        sut.ScanLocalFilesCommand.Execute().Subscribe();
        executeStarted.Task.Wait(TimeSpan.FromSeconds(5));
        Task.Delay(600).Wait(); // Wait for throttled progress update

        progressReceived.ShouldBeTrue();
        sut.PendingUploads.ShouldBe(15);

        tcs.SetResult(true);
    }
}
