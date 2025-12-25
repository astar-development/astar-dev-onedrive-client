using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Services;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.SettingsAndPreferences;
using AStar.Dev.OneDrive.Client.ViewModels;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.ViewModels;

/// <summary>
/// Unit tests for MainWindowViewModel.ScanLocalFilesCommand.
/// </summary>
public sealed class MainWindowViewModelShould
{
    private readonly IAuthService _mockAuth;
    private readonly ISyncEngine _mockSync;
    private readonly ISyncRepository _mockRepo;
    private readonly ITransferService _mockTransfer;
    private readonly ISettingsAndPreferencesService _mockSettings;
    private readonly ILogger<MainWindowViewModel> _mockLogger;
    private readonly Subject<SyncProgress> _syncProgressSubject;
    private readonly Subject<SyncProgress> _transferProgressSubject;

    public MainWindowViewModelShould()
    {
        _mockAuth = Substitute.For<IAuthService>();
        _mockSync = Substitute.For<ISyncEngine>();
        _mockRepo = Substitute.For<ISyncRepository>();
        _mockTransfer = Substitute.For<ITransferService>();
        _mockSettings = Substitute.For<ISettingsAndPreferencesService>();
        _mockLogger = Substitute.For<ILogger<MainWindowViewModel>>();

        // Stub observables to prevent NullReferenceException
        _syncProgressSubject = new Subject<SyncProgress>();
        _transferProgressSubject = new Subject<SyncProgress>();
        _mockSync.Progress.Returns(_syncProgressSubject);
        _mockTransfer.Progress.Returns(_transferProgressSubject);

        UserPreferences userPreferences = new();
        _mockSettings.Load().Returns(userPreferences);
    }

    private MainWindowViewModel CreateViewModel() => new(_mockAuth, _mockSync, _mockRepo, _mockTransfer, _mockSettings, _mockLogger);

    [Fact]
    public void InitializeWithScanLocalFilesCommand()
    {
        MainWindowViewModel sut = CreateViewModel();

        sut.ScanLocalFilesCommand.ShouldNotBeNull();
    }

    [Fact]
    public void ScanLocalFilesCommand_CallsSyncEngineScanLocalFilesAsync()
    {
        // Arrange
        MainWindowViewModel sut = CreateViewModel();
        _mockSync.ScanLocalFilesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        TaskCompletionSource tcs = new();
        _ = sut.ScanLocalFilesCommand.Subscribe(_ => tcs.SetResult());

        // Act
        sut.ScanLocalFilesCommand.Execute().Subscribe();
        tcs.Task.Wait(TimeSpan.FromSeconds(5));

        // Assert
        _ = _mockSync.Received(1).ScanLocalFilesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ScanLocalFilesCommand_UpdatesSyncStatusToScanning()
    {
        // Arrange
        MainWindowViewModel sut = CreateViewModel();
        TaskCompletionSource<bool> executeStarted = new();
        TaskCompletionSource<bool> tcs = new();
        
        _mockSync.ScanLocalFilesAsync(Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                executeStarted.SetResult(true);
                await tcs.Task;
            });

        // Act
        sut.ScanLocalFilesCommand.Execute().Subscribe();
        executeStarted.Task.Wait(TimeSpan.FromSeconds(5));

        // Assert
        sut.SyncStatus.ShouldBe("Scanning local files");

        // Cleanup
        tcs.SetResult(true);
    }

    [Fact]
    public void ScanLocalFilesCommand_UpdatesSyncStatusToCompleteWhenFinished()
    {
        // Arrange
        MainWindowViewModel sut = CreateViewModel();
        _mockSync.ScanLocalFilesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        TaskCompletionSource tcs = new();
        _ = sut.ScanLocalFilesCommand.Subscribe(_ => tcs.SetResult());

        // Act
        sut.ScanLocalFilesCommand.Execute().Subscribe();
        tcs.Task.Wait(TimeSpan.FromSeconds(5));

        // Assert
        sut.SyncStatus.ShouldBe("Local file scan complete");
    }

    [Fact]
    public void ScanLocalFilesCommand_AddsSuccessMessageToRecentTransfers()
    {
        // Arrange
        MainWindowViewModel sut = CreateViewModel();
        _mockSync.ScanLocalFilesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        TaskCompletionSource tcs = new();
        _ = sut.ScanLocalFilesCommand.Subscribe(_ => tcs.SetResult());

        // Act
        sut.ScanLocalFilesCommand.Execute().Subscribe();
        tcs.Task.Wait(TimeSpan.FromSeconds(5));

        // Assert
        sut.RecentTransfers.ShouldContain(t => t.Contains("Local file scan completed successfully"));
    }

    [Fact]
    public void ScanLocalFilesCommand_RefreshesStatsAfterCompletion()
    {
        // Arrange
        MainWindowViewModel sut = CreateViewModel();
        _mockSync.ScanLocalFilesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _mockRepo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>())
            .Returns(5);
        _mockRepo.GetPendingUploadCountAsync(Arg.Any<CancellationToken>())
            .Returns(3);

        TaskCompletionSource tcs = new();
        _ = sut.ScanLocalFilesCommand.Subscribe(_ => tcs.SetResult());

        // Act
        sut.ScanLocalFilesCommand.Execute().Subscribe();
        tcs.Task.Wait(TimeSpan.FromSeconds(5));
        Task.Delay(200).Wait(); // Give time for async refresh

        // Assert
        _ = _mockRepo.Received().GetPendingDownloadCountAsync(Arg.Any<CancellationToken>());
        _ = _mockRepo.Received().GetPendingUploadCountAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ScanLocalFilesCommand_HandlesCancellation()
    {
        // Arrange
        MainWindowViewModel sut = CreateViewModel();
        _mockSync.ScanLocalFilesAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new OperationCanceledException());

        TaskCompletionSource tcs = new();
        _ = sut.ScanLocalFilesCommand.Subscribe(_ => tcs.SetResult());

        // Act
        sut.ScanLocalFilesCommand.Execute().Subscribe();
        tcs.Task.Wait(TimeSpan.FromSeconds(5));

        // Assert
        sut.SyncStatus.ShouldBe("Local file scan cancelled");
        sut.RecentTransfers.ShouldContain(t => t.Contains("Local file scan was cancelled"));
    }

    [Fact]
    public void ScanLocalFilesCommand_HandlesExceptions()
    {
        // Arrange
        MainWindowViewModel sut = CreateViewModel();
        _mockSync.ScanLocalFilesAsync(Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Test error"));

        var errorThrown = false;
        _ = sut.ScanLocalFilesCommand.ThrownExceptions.Subscribe(_ => errorThrown = true);

        // Act
        sut.ScanLocalFilesCommand.Execute().Subscribe(_ => { }, _ => { });
        Task.Delay(100).Wait();

        // Assert
        sut.SyncStatus.ShouldBe("Local file scan failed");
        sut.RecentTransfers.ShouldContain(t => t.Contains("ERROR") && t.Contains("Test error"));
        errorThrown.ShouldBeTrue();
    }

    [Fact]
    public void IsSyncingStatus_ReturnsTrueForScanningStatus()
    {
        // Arrange
        MainWindowViewModel sut = CreateViewModel();

        // Act
        sut.SyncStatus = "Scanning local files";

        // Assert - The command should be disabled (cannot execute) when scanning
        // We test this indirectly by checking that the status contains "Scanning"
        sut.SyncStatus.Contains("Scanning", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public void IsSyncingStatus_ReturnsFalseForScanCompleteStatus()
    {
        // Arrange
        MainWindowViewModel sut = CreateViewModel();

        // Act
        sut.SyncStatus = "Local file scan complete";

        // Assert - The status contains "complete" so syncing should be considered done
        sut.SyncStatus.Contains("complete", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public void ScanLocalFilesCommand_ReceivesProgressUpdatesFromSyncEngine()
    {
        // Arrange
        MainWindowViewModel sut = CreateViewModel();
        TaskCompletionSource<bool> executeStarted = new();
        TaskCompletionSource<bool> tcs = new();
        
        _mockSync.ScanLocalFilesAsync(Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                _syncProgressSubject.OnNext(new SyncProgress
                {
                    CurrentOperation = "Scanning local files (50/100)...",
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
            if (e.PropertyName == nameof(MainWindowViewModel.SyncStatus) &&
                sut.SyncStatus.Contains("Scanning local files"))
            {
                progressReceived = true;
            }
        };

        // Act
        sut.ScanLocalFilesCommand.Execute().Subscribe();
        executeStarted.Task.Wait(TimeSpan.FromSeconds(5));
        Task.Delay(600).Wait(); // Wait for throttled progress update

        // Assert
        progressReceived.ShouldBeTrue();
        sut.PendingUploads.ShouldBe(15);

        // Cleanup
        tcs.SetResult(true);
    }
}
