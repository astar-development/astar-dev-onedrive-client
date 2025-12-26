using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Services;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.SettingsAndPreferences;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IAuthService _auth;
    private readonly ISyncEngine _sync;
    private readonly ISyncRepository _repo;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly CompositeDisposable _disposables = [];
    private CancellationTokenSource? _currentSyncCancellation;

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

    // Performance metrics
    public string TransferSpeed { get; set => this.RaiseAndSetIfChanged(ref field, value); } = string.Empty;
    public string EstimatedTimeRemaining { get; set => this.RaiseAndSetIfChanged(ref field, value); } = string.Empty;
    public string ElapsedTime { get; set => this.RaiseAndSetIfChanged(ref field, value); } = string.Empty;

    private const int MaxRecentTransfers = 15;

    public MainWindowViewModel(IAuthService auth, ISyncEngine sync, ISyncRepository repo, ITransferService transfer,
      ISettingsAndPreferencesService settingsAndPreferencesService, ILogger<MainWindowViewModel> logger)
    {
        _auth = auth;
        _sync = sync;
        _repo = repo;
        _logger = logger;
        UserPreferences = settingsAndPreferencesService.Load();

        IObservable<bool> isSyncing = this.WhenAnyValue(x => x.OperationType)
            .Select(type => type == SyncOperationType.Syncing);

        SignInCommand = SignIn();
        InitialSyncCommand = CreateInitialSyncCommand(isSyncing);
        IncrementalSyncCommand = CreateIncrementalSyncCommand(isSyncing);
        CancelSyncCommand = CreateCancelSyncCommand(isSyncing);
        ScanLocalFilesCommand = CreateScanLocalFilesCommand(isSyncing);

        SubscribeToSyncProgress();
        SubscribeToTransferProgress(transfer);
    }

    private void SubscribeToTransferProgress(ITransferService transfer) => _ = transfer.Progress
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

    private void SubscribeToSyncProgress() => _ = _sync.Progress
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

    private ReactiveCommand<Unit, Unit> CreateCancelSyncCommand(IObservable<bool> isSyncing) => ReactiveCommand.Create(() =>
    {
        _currentSyncCancellation?.Cancel();
        SyncStatusMessage = "Cancelling...";
        AddRecentTransfer($"{DateTimeOffset.Now:HH:mm:ss} - Sync cancellation requested");
    }, isSyncing);

    private ReactiveCommand<Unit, Unit> CreateScanLocalFilesCommand(IObservable<bool> isSyncing) => ReactiveCommand.CreateFromTask(async ct =>
    {
        _currentSyncCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        try
        {
            SyncStatusMessage = $"Processing {OperationConstants.LocalFileSync}...";
            ProgressPercent = 0;
            AddRecentTransfer($"Processing {OperationConstants.LocalFileSync}...");
            await _sync.ScanLocalFilesAsync(_currentSyncCancellation.Token);
            SyncStatusMessage = $"{OperationConstants.LocalFileSync} completed successfully";
            ProgressPercent = 100;
            _ = RefreshStatsAsync();
            AddRecentTransfer($"{OperationConstants.LocalFileSync} completed successfully");
            _logger.LogInformation($"{OperationConstants.LocalFileSync} completed successfully");
        }
        catch(OperationCanceledException)
        {
            UpdateCancelled(OperationConstants.LocalFileSync);
        }
        catch(Exception ex)
        {
            UpdateFailed(OperationConstants.LocalFileSync, ex);
        }
        finally
        {
            _currentSyncCancellation?.Dispose();
            _currentSyncCancellation = null;
        }
    }, isSyncing.Select(syncing => !syncing));

    private ReactiveCommand<Unit, Unit> CreateIncrementalSyncCommand(IObservable<bool> isSyncing) => ReactiveCommand.CreateFromTask(async ct =>
    {
        _currentSyncCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        try
        {
            SyncStatusMessage = $"Running {OperationConstants.IncrementalFileSync}";
            ProgressPercent = 0;
            await _sync.IncrementalSyncAsync(_currentSyncCancellation.Token);
            SyncStatusMessage = $"{OperationConstants.IncrementalFileSync} complete";
            ProgressPercent = 100;
            _ = RefreshStatsAsync();
            AddRecentTransfer($"{OperationConstants.IncrementalFileSync} completed successfully");
            _logger.LogInformation($"{OperationConstants.IncrementalFileSync} completed successfully");
        }
        catch(OperationCanceledException)
        {
            UpdateCancelled(OperationConstants.IncrementalFileSync);
        }
        catch(InvalidOperationException ex) when(ex.Message.Contains("Delta token missing"))
        {
            SyncStatusMessage = $"{OperationConstants.IncrementalFileSync} failed";
            ProgressPercent = 0;
            var errorMsg = "Must run initial sync before incremental sync";
            AddRecentTransfer($"ERROR: {errorMsg}");
            _logger.LogWarning(ex, "Incremental sync attempted before initial sync");
        }
        catch(Exception ex)
        {
            UpdateFailed(OperationConstants.IncrementalFileSync, ex);
        }
        finally
        {
            _currentSyncCancellation?.Dispose();
            _currentSyncCancellation = null;
        }
    }, isSyncing.Select(syncing => !syncing));

    private ReactiveCommand<Unit, Unit> CreateInitialSyncCommand(IObservable<bool> isSyncing) => ReactiveCommand.CreateFromTask(async ct =>
    {
        _currentSyncCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        try
        {
            SyncStatusMessage = $"Running {OperationConstants.InitialFileSync}";
            ProgressPercent = 0;
            await _sync.InitialFullSyncAsync(_currentSyncCancellation.Token);
            SyncStatusMessage = $"{OperationConstants.InitialFileSync} complete";
            ProgressPercent = 100;
            _ = RefreshStatsAsync();
            AddRecentTransfer($"{OperationConstants.InitialFileSync} completed successfully");
            _logger.LogInformation($"{OperationConstants.InitialFileSync} completed successfully");
        }
        catch(OperationCanceledException)
        {
            UpdateCancelled(OperationConstants.InitialFileSync);
        }
        catch(InvalidOperationException ex)
        {
            SyncStatusMessage = $"{OperationConstants.InitialFileSync} failed";
            ProgressPercent = 0;
            var errorMsg = $"Configuration error: {ex.Message}";
            AddRecentTransfer($"ERROR: {errorMsg}");
            _logger.LogError(ex, $"{OperationConstants.InitialFileSync} failed due to configuration error");
        }
        catch(Exception ex)
        {
            UpdateFailed(OperationConstants.InitialFileSync, ex);
        }
        finally
        {
            _currentSyncCancellation?.Dispose();
            _currentSyncCancellation = null;
        }
    }, isSyncing.Select(syncing => !syncing));

    private void UpdateCancelled(string operation)
    {
        SyncStatusMessage = $"{operation} cancelled";
        ProgressPercent = 0;
        AddRecentTransfer($"{operation} was cancelled");
        _logger.LogInformation("{OperationName} was cancelled by user", operation);
    }

    private void UpdateFailed(string operation, Exception ex)
    {
        SyncStatusMessage = $"{operation} failed";
        ProgressPercent = 0;
        AddRecentTransfer($"{operation} failed");
        var errorMsg = $"Sync error: {ex.Message}";
        AddRecentTransfer($"ERROR: {errorMsg}");
        _logger.LogError(ex, "{OperationName} failed", operation);
    }

    private ReactiveCommand<Unit, Unit> SignIn() => ReactiveCommand.CreateFromTask(async ct =>
    {
        try
        {
            SyncStatusMessage = "Signing in...";
            await _auth.SignInAsync(ct);
            SyncStatusMessage = "Signed in";
            SignedIn = true;
            RecentTransfers.Add($"Signed in at {DateTimeOffset.Now}");
            _logger.LogInformation("User successfully signed in");
        }
        catch(Exception ex)
        {
            SyncStatusMessage = "Sign-in failed";
            SignedIn = false;
            var errorMsg = $"Sign-in failed: {ex.Message}";
            RecentTransfers.Insert(0, $"{DateTimeOffset.Now:HH:mm:ss} - ERROR: {errorMsg}");
            _logger.LogError(ex, "Sign-in failed");
        }
    });

    private void UpdatePerformanceMetrics(SyncProgress progress)
    {
        TransferSpeed = progress.BytesPerSecond > 0 ? $"{progress.MegabytesPerSecond:F2} MB/s" : string.Empty;

        if(progress.EstimatedTimeRemaining.HasValue)
        {
            TimeSpan eta = progress.EstimatedTimeRemaining.Value;
            DisplayEta(eta);
        }
        else
        {
            EstimatedTimeRemaining = string.Empty;
        }

        if(progress.ElapsedTime.TotalSeconds > 0)
        {
            TimeSpan elapsed = progress.ElapsedTime;
            DisplayElapsedTime(elapsed);
        }
        else
        {
            ElapsedTime = string.Empty;
        }
    }

    private void DisplayElapsedTime(TimeSpan elapsedTime) => ElapsedTime = elapsedTime.TotalHours >= 1
                    ? $"{elapsedTime.Hours}h {elapsedTime.Minutes}m {elapsedTime.Seconds}s"
                    : BuildMessageFor(elapsedTime);

    private void DisplayEta(TimeSpan eta) => EstimatedTimeRemaining = eta.TotalHours >= 1
                    ? $"ETA: {eta.Hours}h {eta.Minutes}m"
                    : BuildMessageFor(eta);

    private static string BuildMessageFor(TimeSpan elapsed) => elapsed.TotalMinutes >= 1
                            ? $"{elapsed.Minutes}m {elapsed.Seconds}s"
                            : $"{elapsed.Seconds}s";

    private async Task RefreshStatsAsync()
    {
        try
        {
            PendingDownloads = await _repo.GetPendingDownloadCountAsync(CancellationToken.None);
            PendingUploads = await _repo.GetPendingUploadCountAsync(CancellationToken.None);
            ProgressPercent = 100;

            _logger.LogDebug("Refreshed stats: {Downloads} pending downloads, {Uploads} pending uploads",
                PendingDownloads, PendingUploads);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh statistics");
            PendingDownloads = 0;
            PendingUploads = 0;
            ProgressPercent = 100;
        }
    }

    private void AddRecentTransfer(string message)
    {
        RecentTransfers.Insert(0, message);
        while(RecentTransfers.Count > MaxRecentTransfers)
        {
            RecentTransfers.RemoveAt(RecentTransfers.Count - 1);
        }
    }

    public void Dispose()
    {
        _disposables?.Dispose();
        _currentSyncCancellation?.Dispose();
    }
}
