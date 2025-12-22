using System.Collections.ObjectModel;
using System.Reactive;
using AStar.Dev.OneDrive.Client.Common;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Services;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.Theme;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly SyncEngine _sync;
    private readonly SyncSettings _settings;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly TransferService _transfer;
    private readonly ApplicationSettings _applicationSettings;
    private readonly IThemeSelectionHandler _themeHandler;
    private readonly IAutoSaveService _autoSaveService;

    public ReactiveCommand<Unit, Unit> SignInCommand { get; }
    public ReactiveCommand<Unit, Unit> InitialSyncCommand { get; }
    public ReactiveCommand<Unit, Unit> IncrementalSyncCommand { get; }

    public ObservableCollection<string> RecentTransfers { get; } = new();
    public int PendingDownloads { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public int PendingUploads { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public string SyncStatus { get; set => this.RaiseAndSetIfChanged(ref field, value); } = "Idle";
    public double ProgressPercent { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public int ParallelDownloads { get; set { _ = this.RaiseAndSetIfChanged(ref field, value); UpdateSettings(); } }
    public int BatchSize { get; set { _ = this.RaiseAndSetIfChanged(ref field, value); UpdateSettings(); } }

    public MainWindowViewModel(IAuthService auth, SyncEngine sync, TransferService transfer, SyncSettings settings,
        ApplicationSettings applicationSettings,
        IThemeSelectionHandler themeHandler,
        IAutoSaveService autoSaveService, ILogger<MainWindowViewModel> logger)
    {
        _auth = auth;
        _sync = sync;
        _transfer = transfer;
        _settings = settings;
        _logger = logger;
        _applicationSettings = applicationSettings;
        _themeHandler = themeHandler;
        _autoSaveService = autoSaveService;

        ParallelDownloads = settings.ParallelDownloads;
        BatchSize = settings.BatchSize;

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
            RefreshStatsAsync();
        });

        IncrementalSyncCommand = ReactiveCommand.CreateFromTask(async ct =>
        {
            SyncStatus = "Running incremental sync";
            await _sync.IncrementalSyncAsync(ct);
            SyncStatus = "Incremental sync complete";
            RefreshStatsAsync();
        });
    }
    private void UpdateSettings()
    {
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
}
