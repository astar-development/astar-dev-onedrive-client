using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Services;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.Services.Syncronisation;
using AStar.Dev.OneDrive.Client.SettingsAndPreferences;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable, ISyncStatusTarget
{
    private readonly ISyncronisationCoordinator _syncCoordinator;
    private readonly CompositeDisposable _disposables = [];

    public ReactiveCommand<Unit, Unit> SignInCommand { get; }
    public ReactiveCommand<Unit, Unit> InitialSyncCommand { get; }
    public ReactiveCommand<Unit, Unit> IncrementalSyncCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelSyncCommand { get; }
    public ReactiveCommand<Unit, Unit> ScanLocalFilesCommand { get; }

    public ObservableCollection<string> RecentTransfers { get; } = [];
    public int PendingDownloads { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public int PendingUploads { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public string SyncStatusMessage { get; set => this.RaiseAndSetIfChanged(ref field, value); } = "Idle";
    public SyncOperationType OperationType { get; set => this.RaiseAndSetIfChanged(ref field, value); } = SyncOperationType.Idle;
    public double ProgressPercent { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public UserPreferences UserPreferences { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public bool SignedIn { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public bool FullSync { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public bool IncrementalSync { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public string TransferSpeed { get; set => this.RaiseAndSetIfChanged(ref field, value); } = string.Empty;
    public string EstimatedTimeRemaining { get; set => this.RaiseAndSetIfChanged(ref field, value); } = string.Empty;
    public string ElapsedTime { get; set => this.RaiseAndSetIfChanged(ref field, value); } = string.Empty;

    public string ApplicationName => ApplicationMetadata.ApplicationName;

    private const int MaxRecentTransfers = 15;

    public MainWindowViewModel(ISyncCommandService syncCommandService, ISyncronisationCoordinator syncCoordinator,
      ISettingsAndPreferencesService settingsAndPreferencesService, IAuthService authService)
    {
        _syncCoordinator = syncCoordinator;
        UserPreferences = settingsAndPreferencesService.Load();
        SignedIn = authService.IsUserSignedInAsync(CancellationToken.None).GetAwaiter().GetResult();
        IObservable<bool> isSyncing = this.WhenAnyValue(x => x.OperationType)
            .Select(type => type == SyncOperationType.Syncing);

        SignInCommand = syncCommandService.CreateSignInCommand(this);
        InitialSyncCommand = syncCommandService.CreateInitialSyncCommand(this, isSyncing);
        DeltaToken? fullSync = _syncCoordinator.GetDeltaTokenAsync(CancellationToken.None).GetAwaiter().GetResult();
        IncrementalSyncCommand = syncCommandService.CreateIncrementalSyncCommand(fullSync ?? new DeltaToken(string.Empty, string.Empty, DateTimeOffset.MinValue), this, isSyncing);
        CancelSyncCommand = syncCommandService.CreateCancelSyncCommand(this, isSyncing);
        ScanLocalFilesCommand = syncCommandService.CreateScanLocalFilesCommand(this, isSyncing);

        SetFullSync(fullSync == null);
        SetIncrementalSync(fullSync != null);
        SubscribeToSyncProgress();
        SubscribeToTransferProgress(_syncCoordinator);
    }

    private void SubscribeToTransferProgress(ISyncronisationCoordinator syncCoordinator) => _ = syncCoordinator.TransferProgress
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(progress =>
                {
                    ProgressPercent = progress.PercentComplete;
                    PendingDownloads = progress.PendingDownloads;
                    PendingUploads = progress.PendingUploads;

                    UpdatePerformanceMetrics(progress);

                    if(progress.ProcessedFiles % 100 == 0)
                    {
                        AddRecentTransfer($"{progress.Timestamp:HH:mm:ss} - {progress.CurrentOperationMessage}");
                    }
                })
                .DisposeWith(_disposables);

    private void SubscribeToSyncProgress() => _ = _syncCoordinator.SyncProgress
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(progress =>
                {
                    OperationType = progress.OperationType;
                    SyncStatusMessage = progress.CurrentOperationMessage;
                    ProgressPercent = progress.PercentComplete;
                    PendingDownloads = progress.PendingDownloads;
                    PendingUploads = progress.PendingUploads;

                    UpdatePerformanceMetrics(progress);

                    AddRecentTransfer($"{progress.Timestamp:HH:mm:ss} - {progress.CurrentOperationMessage} ({progress.ProcessedFiles}/{progress.TotalFiles})");
                })
                .DisposeWith(_disposables);

    public void SetStatus(string status) => SyncStatusMessage = status;
    public void SetProgress(double percent) => ProgressPercent = percent;
    public void AddRecentTransfer(string message) => AddRecentTransferInternal(message);
    public void OnSyncCompleted() => _ = RefreshStatsAsync();
    public void OnSyncFailed(string operation, Exception ex)
    {
        SyncStatusMessage = $"{operation} failed";
        ProgressPercent = 0d;
        AddRecentTransferInternal($"{operation} failed");
        AddRecentTransferInternal($"ERROR: Sync error: {ex.Message}");
    }
    public void OnSyncCancelled(string operation)
    {
        SyncStatusMessage = $"{operation} cancelled";
        ProgressPercent = 0d;
        AddRecentTransferInternal($"{operation} was cancelled");
    }

    public void SetSignedIn(bool value) => SignedIn = value;

    public void SetFullSync(bool value) => FullSync = value;

    public void SetIncrementalSync(bool value) => IncrementalSync = value;

    private void UpdatePerformanceMetrics(SyncProgress progress)
    {
        TransferSpeed = progress.BytesPerSecond > 0 ? $"{progress.MegabytesPerSecond:F2} MB/s" : string.Empty;
        EstimatedTimeRemaining = progress.EstimatedTimeRemaining.HasValue ? BuildEta(progress.EstimatedTimeRemaining.Value) : string.Empty;
        ElapsedTime = progress.ElapsedTime.TotalSeconds > 0 ? BuildElapsed(progress.ElapsedTime) : string.Empty;
    }

    private static string BuildElapsed(TimeSpan elapsedTime)
        => FormatElapsed(elapsedTime);

    private static string BuildEta(TimeSpan eta)
        => FormatEta(eta);

    private static string FormatElapsed(TimeSpan elapsedTime) => elapsedTime.TotalHours switch
    {
        >= 1 => $"{elapsedTime.Hours}h {elapsedTime.Minutes}m {elapsedTime.Seconds}s",
        _ => elapsedTime.TotalMinutes switch
        {
            >= 1 => $"{elapsedTime.Minutes}m {elapsedTime.Seconds}s",
            _ => $"{elapsedTime.Seconds}s"
        }
    };

    private static string FormatEta(TimeSpan eta) => eta.TotalHours switch
    {
        >= 1 => $"ETA: {eta.Hours}h {eta.Minutes}m",
        _ => eta.TotalMinutes switch
        {
            >= 1 => $"ETA: {eta.Minutes}m {eta.Seconds}s",
            _ => $"ETA: {eta.Seconds}s"
        }
    };

    private async Task RefreshStatsAsync()
        => (PendingDownloads, PendingUploads, ProgressPercent) =
            await TryGetStatsAsync() ?? (0, 0, 100d);

    private async Task<(int, int, double)?> TryGetStatsAsync()
    {
        try
        {
            var downloads = await _syncCoordinator.GetPendingDownloadCountAsync(CancellationToken.None);
            var uploads = await _syncCoordinator.GetPendingUploadCountAsync(CancellationToken.None);
            return (downloads, uploads, 100d);
        }
        catch
        {
            return null;
        }
    }

    private void AddRecentTransferInternal(string message)
    {
        RecentTransfers.Insert(0, message);
        while(RecentTransfers.Count > MaxRecentTransfers)
        {
            RecentTransfers.RemoveAt(RecentTransfers.Count - 1);
        }
    }

    public void Dispose() => _disposables?.Dispose();
}
