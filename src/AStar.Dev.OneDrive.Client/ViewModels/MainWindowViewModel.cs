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
    public string SyncStatus { get; set => this.RaiseAndSetIfChanged(ref field, value); } = "Idle";
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

        SignInCommand = ReactiveCommand.CreateFromTask(async ct =>
        {
            try
            {
                SyncStatus = "Signing in...";
                await _auth.SignInAsync(ct);
                SyncStatus = "Signed in";
                SignedIn = true;
                RecentTransfers.Add($"Signed in at {DateTimeOffset.Now}");
                _logger.LogInformation("User successfully signed in");
            }
            catch (Exception ex)
            {
                SyncStatus = "Sign-in failed";
                SignedIn = false;
                var errorMsg = $"Sign-in failed: {ex.Message}";
                RecentTransfers.Insert(0, $"{DateTimeOffset.Now:HH:mm:ss} - ERROR: {errorMsg}");
                _logger.LogError(ex, "Sign-in failed");
                throw;
            }
        });

        IObservable<bool> isSyncing = this.WhenAnyValue(x => x.SyncStatus).Select(IsSyncingStatus);

                InitialSyncCommand = ReactiveCommand.CreateFromTask(async ct =>
                {
                    _currentSyncCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    try
                    {
                        SyncStatus = "Running initial full sync";
                        await _sync.InitialFullSyncAsync(_currentSyncCancellation.Token);
                        SyncStatus = "Initial sync complete";
                        _ = RefreshStatsAsync();
                        AddRecentTransfer("Initial sync completed successfully");
                        _logger.LogInformation("Initial sync completed successfully");
                    }
                    catch(OperationCanceledException)
                    {
                        SyncStatus = "Initial sync cancelled";
                        AddRecentTransfer("Initial sync was cancelled");
        #pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
                        _logger.LogInformation("Initial sync was cancelled by user");
        #pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.
                    }
                    catch(InvalidOperationException ex)
                    {
                        SyncStatus = "Initial sync failed - missing configuration";
                        var errorMsg = $"Configuration error: {ex.Message}";
                        AddRecentTransfer($"ERROR: {errorMsg}");
                        _logger.LogError(ex, "Initial sync failed due to configuration error");
                        throw;
                    }
                    catch(Exception ex)
                    {
                        SyncStatus = "Initial sync failed";
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

                IncrementalSyncCommand = ReactiveCommand.CreateFromTask(async ct =>
                {
                    _currentSyncCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    try
                    {
                        SyncStatus = "Running incremental sync";
                        await _sync.IncrementalSyncAsync(_currentSyncCancellation.Token);
                        SyncStatus = "Incremental sync complete";
                        _ = RefreshStatsAsync();
                        AddRecentTransfer("Incremental sync completed successfully");
                        _logger.LogInformation("Incremental sync completed successfully");
                    }
                    catch(OperationCanceledException)
                    {
                        SyncStatus = "Incremental sync cancelled";
                        AddRecentTransfer("Incremental sync was cancelled");
        #pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
                        _logger.LogInformation("Incremental sync was cancelled by user");
        #pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.
                    }
                    catch(InvalidOperationException ex) when (ex.Message.Contains("Delta token missing"))
                    {
                        SyncStatus = "Incremental sync failed - run initial sync first";
                        var errorMsg = "Must run initial sync before incremental sync";
                        AddRecentTransfer($"ERROR: {errorMsg}");
                        _logger.LogWarning(ex, "Incremental sync attempted before initial sync");
                        throw;
                    }
                    catch(Exception ex)
                    {
                        SyncStatus = "Incremental sync failed";
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

                CancelSyncCommand = ReactiveCommand.Create(() =>
                {
                    _currentSyncCancellation?.Cancel();
                    SyncStatus = "Cancelling...";
                    AddRecentTransfer($"{DateTimeOffset.Now:HH:mm:ss} - Sync cancellation requested");
                }, isSyncing);

            ScanLocalFilesCommand = ReactiveCommand.CreateFromTask(async ct =>
            {
                _currentSyncCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
                try
                {
                    ProgressPercent = 0;
                    await _sync.ScanLocalFilesAsync(_currentSyncCancellation.Token);
                    ProgressPercent = 100;
                    _ = RefreshStatsAsync();
                    AddRecentTransfer("Local file scan completed successfully");
                    _logger.LogInformation("Local file scan completed successfully");
                }
                catch (OperationCanceledException)
                {
                    ProgressPercent = 0;
                    AddRecentTransfer("Local file scan was cancelled");
#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
                    _logger.LogInformation("Local file scan was cancelled by user");
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.
                }
                catch (Exception ex)
                {
                    ProgressPercent = 0;
                    var errorMsg = $"Scan error: {ex.Message}";
                    AddRecentTransfer($"ERROR: {errorMsg}");
                    _logger.LogError(ex, "Local file scan failed");
                    throw;
                }
                finally
                {
                    _currentSyncCancellation?.Dispose();
                    _currentSyncCancellation = null;
                }
            }, isSyncing.Select(syncing => !syncing));

            // Subscribe to SyncEngine progress
            _ = _sync.Progress
                .Throttle(TimeSpan.FromMilliseconds(500)) // Throttle to avoid UI flooding
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(progress =>
                {
                    SyncStatus = progress.CurrentOperation;
                    ProgressPercent = progress.PercentComplete;
                    PendingDownloads = progress.PendingDownloads;
                    PendingUploads = progress.PendingUploads;

                    // Update performance metrics
                    UpdatePerformanceMetrics(progress);

                    AddRecentTransfer($"{progress.Timestamp:HH:mm:ss} - {progress.CurrentOperation} ({progress.ProcessedFiles}/{progress.TotalFiles})");
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
                        AddRecentTransfer($"{progress.Timestamp:HH:mm:ss} - {progress.CurrentOperation}");
                    }
                })
                .DisposeWith(_disposables);
        }

    private static bool IsSyncingStatus(string status) => (status.Contains("sync", StringComparison.OrdinalIgnoreCase) || 
                                 status.Contains("Processing", StringComparison.OrdinalIgnoreCase) ||
                                 status.Contains("Scanning", StringComparison.OrdinalIgnoreCase)) &&
                                 !status.Contains("complete", StringComparison.OrdinalIgnoreCase);

    private void UpdatePerformanceMetrics(SyncProgress progress)
    {
        // Transfer speed
        if (progress.BytesPerSecond > 0)
        {
            TransferSpeed = $"{progress.MegabytesPerSecond:F2} MB/s";
        }
        else
        {
            TransferSpeed = string.Empty;
        }

        // Estimated time remaining
        if (progress.EstimatedTimeRemaining.HasValue)
        {
            var eta = progress.EstimatedTimeRemaining.Value;
            EstimatedTimeRemaining = eta.TotalHours >= 1 
                ? $"ETA: {eta.Hours}h {eta.Minutes}m" 
                : eta.TotalMinutes >= 1
                    ? $"ETA: {eta.Minutes}m {eta.Seconds}s"
                    : $"ETA: {eta.Seconds}s";
        }
        else
        {
            EstimatedTimeRemaining = string.Empty;
        }

        // Elapsed time
        if (progress.ElapsedTime.TotalSeconds > 0)
        {
            var elapsed = progress.ElapsedTime;
            ElapsedTime = elapsed.TotalHours >= 1
                ? $"{elapsed.Hours}h {elapsed.Minutes}m {elapsed.Seconds}s"
                : elapsed.TotalMinutes >= 1
                    ? $"{elapsed.Minutes}m {elapsed.Seconds}s"
                    : $"{elapsed.Seconds}s";
        }
        else
        {
            ElapsedTime = string.Empty;
        }
    }

    private async Task RefreshStatsAsync()
    {
        try
        {
            // Query repository for actual counts
            PendingDownloads = await _repo.GetPendingDownloadCountAsync(CancellationToken.None);
            PendingUploads = await _repo.GetPendingUploadCountAsync(CancellationToken.None);
            ProgressPercent = 100;

            _logger.LogDebug("Refreshed stats: {Downloads} pending downloads, {Uploads} pending uploads",
                PendingDownloads, PendingUploads);
        }
        catch (Exception ex)
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
