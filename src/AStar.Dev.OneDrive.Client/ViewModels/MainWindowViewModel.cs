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
    private readonly SyncEngine _sync;
    private readonly SyncSettings _settings;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly CompositeDisposable _disposables = [];
    private CancellationTokenSource? _currentSyncCancellation;

    public ReactiveCommand<Unit, Unit> SignInCommand { get; }
    public ReactiveCommand<Unit, Unit> InitialSyncCommand { get; }
    public ReactiveCommand<Unit, Unit> IncrementalSyncCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelSyncCommand { get; }

    public ObservableCollection<string> RecentTransfers { get; } = [];
    public int PendingDownloads { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public int PendingUploads { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public string SyncStatus { get; set => this.RaiseAndSetIfChanged(ref field, value); } = "Idle";
    public double ProgressPercent { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public UserPreferences UserPreferences { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public int ParallelDownloads { get; set { _ = this.RaiseAndSetIfChanged(ref field, value); UpdateSettings(); } }
    public int BatchSize { get; set { _ = this.RaiseAndSetIfChanged(ref field, value); UpdateSettings(); } }
    public bool SignedIn { get; set => this.RaiseAndSetIfChanged(ref field, value); }

    private const int MaxRecentTransfers = 15;

    public MainWindowViewModel(IAuthService auth, SyncEngine sync, TransferService transfer, SyncSettings settings,
      ISettingsAndPreferencesService settingsAndPreferencesService, ILogger<MainWindowViewModel> logger)
    {
        _auth = auth;
        _sync = sync;
        _settings = settings;
        _logger = logger;
        UserPreferences = settingsAndPreferencesService.Load();

        ParallelDownloads = settings.MaxParallelDownloads;
        BatchSize = settings.DownloadBatchSize;

        SignInCommand = ReactiveCommand.CreateFromTask(async ct =>
        {
            SyncStatus = "Signing in...";
            await _auth.SignInAsync(ct);
            SyncStatus = "Signed in";
            SignedIn = true;
        });

        IObservable<bool> isSyncing = this.WhenAnyValue(x => x.SyncStatus)
            .Select(status => (status.Contains("sync", StringComparison.OrdinalIgnoreCase) || status.Contains("Processing", StringComparison.OrdinalIgnoreCase)) &&
                             !status.Contains("complete", StringComparison.OrdinalIgnoreCase));

        InitialSyncCommand = ReactiveCommand.CreateFromTask(async ct =>
        {
            _currentSyncCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
            try
            {
                SyncStatus = "Running initial full sync";
                await _sync.InitialFullSyncAsync(_currentSyncCancellation.Token);
                SyncStatus = "Initial sync complete";
                RefreshStatsAsync();
            }
            catch(OperationCanceledException)
            {
                SyncStatus = "Initial sync cancelled";
#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
                _logger.LogInformation("Initial sync was cancelled by user");
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.
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
                RefreshStatsAsync();
            }
            catch(OperationCanceledException)
            {
                SyncStatus = "Incremental sync cancelled";
#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
                _logger.LogInformation("Incremental sync was cancelled by user");
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.
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

                if(progress.ProcessedFiles % 100 == 0) // Log every 100 files
                {
                    AddRecentTransfer($"{progress.Timestamp:HH:mm:ss} - {progress.CurrentOperation}");
                }
            })
            .DisposeWith(_disposables);
    }

    private void UpdateSettings()
    {
        _settings.MaxParallelDownloads = ParallelDownloads;
        _settings.DownloadBatchSize = BatchSize;
        // Update SyncSettings instance used by TransferService
        // For simplicity, this example does not re-create services; in production update via options pattern
    }

    private void RefreshStatsAsync()
    {
        // Query repository for counts and update properties
        // Placeholder: set dummy values
        PendingDownloads = 0;
        PendingUploads = 0;
        ProgressPercent = 100;
        RecentTransfers.Clear();
        RecentTransfers.Add($"Sync completed at {DateTimeOffset.Now}");
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
