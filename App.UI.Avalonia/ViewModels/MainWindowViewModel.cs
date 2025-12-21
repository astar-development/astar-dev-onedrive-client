using ReactiveUI;
using System.Reactive;
using App.Core.Interfaces;
using App.Services;
using Microsoft.Extensions.Logging;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly SyncEngine _sync;
    private readonly SyncSettings _settings;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly TransferService _transfer;

    public ReactiveCommand<Unit, Unit> SignInCommand { get; }
    public ReactiveCommand<Unit, Unit> InitialSyncCommand { get; }
    public ReactiveCommand<Unit, Unit> IncrementalSyncCommand { get; }

    public ObservableCollection<string> RecentTransfers { get; } = new();
    public int PendingDownloads { get => _pendingDownloads; set => this.RaiseAndSetIfChanged(ref _pendingDownloads, value); }
    public int PendingUploads { get => _pendingUploads; set => this.RaiseAndSetIfChanged(ref _pendingUploads, value); }
    public string SyncStatus { get => _syncStatus; set => this.RaiseAndSetIfChanged(ref _syncStatus, value); }
    public double ProgressPercent { get => _progressPercent; set => this.RaiseAndSetIfChanged(ref _progressPercent, value); }

    public bool UseDarkTheme { get => _useDarkTheme; set { this.RaiseAndSetIfChanged(ref _useDarkTheme, value); ApplyTheme(); } }
    public int ParallelDownloads { get => _parallelDownloads; set { this.RaiseAndSetIfChanged(ref _parallelDownloads, value); UpdateSettings(); } }
    public int BatchSize { get => _batchSize; set { this.RaiseAndSetIfChanged(ref _batchSize, value); UpdateSettings(); } }

    private int _pendingDownloads;
    private int _pendingUploads;
    private string _syncStatus = "Idle";
    private double _progressPercent;
    private bool _useDarkTheme;
    private int _parallelDownloads;
    private int _batchSize;

    public MainWindowViewModel(IAuthService auth, SyncEngine sync, TransferService transfer, SyncSettings settings, ILogger<MainWindowViewModel> logger)
    {
        _auth = auth;
        _sync = sync;
        _transfer = transfer;
        _settings = settings;
        _logger = logger;

        _parallelDownloads = settings.ParallelDownloads;
        _batchSize = settings.BatchSize;

        SignInCommand = ReactiveCommand.CreateFromTask(async ct =>
        {
            SyncStatus = "Signing in...";
            await _auth.SignInAsync(ct);
            SyncStatus = "Signed in";
        });

        InitialSyncCommand = ReactiveCommand.CreateFromTask(async ct =>
        {
            SyncStatus = "Running initial full sync";
            await _sync.InitialFullSyncAsync(ct);
            SyncStatus = "Initial sync complete";
            await RefreshStatsAsync(ct);
        });

        IncrementalSyncCommand = ReactiveCommand.CreateFromTask(async ct =>
        {
            SyncStatus = "Running incremental sync";
            await _sync.IncrementalSyncAsync(ct);
            SyncStatus = "Incremental sync complete";
            await RefreshStatsAsync(ct);
        });
    }

    private void ApplyTheme()
    {
        // Minimal theme switcher. For full theme support, swap Avalonia styles.
        // This placeholder toggles a simple resource; extend as needed.
    }

    private void UpdateSettings()
    {
        // Update SyncSettings instance used by TransferService
        // For simplicity, this example does not re-create services; in production update via options pattern
    }

    private async Task RefreshStatsAsync(CancellationToken ct)
    {
        // Query repository for counts and update properties
        // Placeholder: set dummy values
        PendingDownloads = 0;
        PendingUploads = 0;
        ProgressPercent = 100;
        RecentTransfers.Clear();
        RecentTransfers.Add($"Sync completed at {DateTimeOffset.Now}");
    }
}
