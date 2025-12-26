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

#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
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

        SignInCommand = SignIn();

        IObservable<bool> isSyncing = this.WhenAnyValue(x => x.OperationType)
            .Select(type => type == SyncOperationType.Syncing);

        InitialSyncCommand = InitialSync(isSyncing);

        IncrementalSyncCommand = IncrementatlSync(isSyncing);

        CancelSyncCommand = ReactiveCommand.Create(() =>
        {
            _currentSyncCancellation?.Cancel();
            SyncStatusMessage = "Cancelling...";
            AddRecentTransfer($"{DateTimeOffset.Now:HH:mm:ss} - Sync cancellation requested");
        }, isSyncing);

        ScanLocalFilesCommand = ScanLocalFiles(isSyncing);

        // Subscribe to SyncEngine progress
        _ = _sync.Progress
            .Throttle(TimeSpan.FromMilliseconds(500)) // Throttle to avoid UI flooding
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(progress =>
            {
                OperationType = progress.OperationType;
                SyncStatusMessage = progress.CurrentOperationMessage;
                ProgressPercent = progress.PercentComplete;
                PendingDownloads = progress.PendingDownloads;
                PendingUploads = progress.PendingUploads;

                // Update performance metrics
                UpdatePerformanceMetrics(progress);

                AddRecentTransfer($"{progress.Timestamp:HH:mm:ss} - {progress.CurrentOperationMessage} ({progress.ProcessedFiles}/{progress.TotalFiles})");
            })
            .DisposeWith(_disposables);

        // Subscribe to TransferService progress
        _ = transfer.Progress
            .Throttle(TimeSpan.FromMilliseconds(500)) // Throttle to avoid UI flooding
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(progress =>
            {
                ProgressPercent = progress.PercentComplete;
                PendingDownloads = progress.PendingDownloads;
                PendingUploads = progress.PendingUploads;

                // Update performance metrics
                UpdatePerformanceMetrics(progress);

                if(progress.ProcessedFiles % 100 == 0) // Log every 100 files
                {
                    AddRecentTransfer($"{progress.Timestamp:HH:mm:ss} - {progress.CurrentOperationMessage}");
                }
            })
            .DisposeWith(_disposables);
    }

    private ReactiveCommand<Unit, Unit> ScanLocalFiles(IObservable<bool> isSyncing) => ReactiveCommand.CreateFromTask(async ct =>
    {
        _currentSyncCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        try
        {
            SyncStatusMessage = "Processing local file sync...";
            AddRecentTransfer("Processing Local file sync...");
            ProgressPercent = 0;
            await _sync.ScanLocalFilesAsync(_currentSyncCancellation.Token);
            ProgressPercent = 100;
            _ = RefreshStatsAsync();
            AddRecentTransfer("Local file sync completed successfully");
            _logger.LogInformation("Local file sync completed successfully");
        }
        catch(OperationCanceledException)
        {
            ProgressPercent = 0;
            AddRecentTransfer("Local file sync was cancelled");
            _logger.LogInformation("Local file sync was cancelled by user");
        }
        catch(Exception ex)
        {
            ProgressPercent = 0;
            var errorMsg = $"Sync error: {ex.Message}";
            AddRecentTransfer($"ERROR: {errorMsg}");
            _logger.LogError(ex, "Local file sync failed");
            throw;
        }
        finally
        {
            _currentSyncCancellation?.Dispose();
            _currentSyncCancellation = null;
        }
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both
    }, isSyncing.Select(syncing => !syncing));
    private ReactiveCommand<Unit, Unit> IncrementatlSync(IObservable<bool> isSyncing) => ReactiveCommand.CreateFromTask(async ct =>
    {
        _currentSyncCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        try
        {
            SyncStatusMessage = "Running incremental sync";
            await _sync.IncrementalSyncAsync(_currentSyncCancellation.Token);
            SyncStatusMessage = "Incremental sync complete";
            _ = RefreshStatsAsync();
            AddRecentTransfer("Incremental sync completed successfully");
            _logger.LogInformation("Incremental sync completed successfully");
        }
        catch(OperationCanceledException)
        {
            SyncStatusMessage = "Incremental sync cancelled";
            AddRecentTransfer("Incremental sync was cancelled");
            _logger.LogInformation("Incremental sync was cancelled by user");
        }
        catch(InvalidOperationException ex) when(ex.Message.Contains("Delta token missing"))
        {
            SyncStatusMessage = "Incremental sync failed - run initial sync first";
            var errorMsg = "Must run initial sync before incremental sync";
            AddRecentTransfer($"ERROR: {errorMsg}");
            _logger.LogWarning(ex, "Incremental sync attempted before initial sync");
            throw;
        }
        catch(Exception ex)
        {
            SyncStatusMessage = "Incremental sync failed";
            var errorMsg = $"Sync error: {ex.Message}";
            AddRecentTransfer($"ERROR: {errorMsg}");
            _logger.LogError(ex, "Incremental sync failed");
            throw;
        }
        finally
        {
            _currentSyncCancellation?.Dispose();
            _currentSyncCancellation = null;
        }
    }, isSyncing.Select(syncing => !syncing));
    private ReactiveCommand<Unit, Unit> InitialSync(IObservable<bool> isSyncing) => ReactiveCommand.CreateFromTask(async ct =>
    {
        _currentSyncCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        try
        {
            SyncStatusMessage = "Running initial full sync";
            await _sync.InitialFullSyncAsync(_currentSyncCancellation.Token);
            SyncStatusMessage = "Initial sync complete";
            _ = RefreshStatsAsync();
            AddRecentTransfer("Initial sync completed successfully");
            _logger.LogInformation("Initial sync completed successfully");
        }
        catch(OperationCanceledException)
        {
            SyncStatusMessage = "Initial sync cancelled";
            AddRecentTransfer("Initial sync was cancelled");
            _logger.LogInformation("Initial sync was cancelled by user");
        }
        catch(InvalidOperationException ex)
        {
            SyncStatusMessage = "Initial sync failed - missing configuration";
            var errorMsg = $"Configuration error: {ex.Message}";
            AddRecentTransfer($"ERROR: {errorMsg}");
            _logger.LogError(ex, "Initial sync failed due to configuration error");
            throw;
        }
        catch(Exception ex)
        {
            SyncStatusMessage = "Initial sync failed";
            var errorMsg = $"Sync error: {ex.Message}";
            AddRecentTransfer($"ERROR: {errorMsg}");
            _logger.LogError(ex, "Initial sync failed");
            throw;
        }
        finally
        {
            _currentSyncCancellation?.Dispose();
            _currentSyncCancellation = null;
        }
    }, isSyncing.Select(syncing => !syncing));
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
            throw;
        }
    });

    private void UpdatePerformanceMetrics(SyncProgress progress)
    {
        // Transfer speed
        TransferSpeed = progress.BytesPerSecond > 0 ? $"{progress.MegabytesPerSecond:F2} MB/s" : string.Empty;

        // Estimated time remaining
        if(progress.EstimatedTimeRemaining.HasValue)
        {
            TimeSpan eta = progress.EstimatedTimeRemaining.Value;
            DisplayEta(eta);
        }
        else
        {
            EstimatedTimeRemaining = string.Empty;
        }

        // Elapsed time
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
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.
