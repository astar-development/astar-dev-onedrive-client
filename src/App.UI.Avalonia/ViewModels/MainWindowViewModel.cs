using System.Collections.ObjectModel;
using System.Reactive;
using App.Core.Interfaces;
using App.Services;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace App.UI.Avalonia.ViewModels;

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
    public int PendingDownloads { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public int PendingUploads { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public string SyncStatus { get; set => this.RaiseAndSetIfChanged(ref field, value); } = "Idle";
    public double ProgressPercent { get; set => this.RaiseAndSetIfChanged(ref field, value); }

    public bool UseDarkTheme { get; set { _ = this.RaiseAndSetIfChanged(ref field, value); ApplyTheme(); } }
    public int ParallelDownloads { get => _parallelDownloads; set { _ = this.RaiseAndSetIfChanged(ref _parallelDownloads, value); UpdateSettings(); } }
    public int BatchSize { get => _batchSize; set { _ = this.RaiseAndSetIfChanged(ref _batchSize, value); UpdateSettings(); } }


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
