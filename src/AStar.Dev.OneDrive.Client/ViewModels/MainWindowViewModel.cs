using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;
using AStar.Dev.OneDrive.Client.Services;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.Services.Syncronisation;
using AStar.Dev.OneDrive.Client.SettingsAndPreferences;
using AStar.Dev.OneDrive.Client.SyncConflicts;
using AStar.Dev.OneDrive.Client.Views;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable, ISyncStatusTarget
{
    private readonly ISyncronisationCoordinator _syncCoordinator;
    private readonly CompositeDisposable _disposables = [];
    private readonly ISyncConflictRepository _conflictRepository;

    public ReactiveCommand<Unit, Unit> SignInCommand { get; }
    public ReactiveCommand<Unit, Unit> InitialSyncCommand { get; }
    public ReactiveCommand<Unit, Unit> IncrementalSyncCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelSyncCommand { get; }
    public ReactiveCommand<Unit, Unit> ScanLocalFilesCommand { get; }

    /// <summary>
    /// Gets the sync progress view model (when sync is active).
    /// </summary>
    public SyncProgressViewModel? SyncProgress
    {
        get;
#pragma warning disable S1144 // Unused private types or members should be removed - it is used in V3... not implemented here yet
        private set => this.RaiseAndSetIfChanged(ref field, value);
#pragma warning restore S1144 // Unused private types or members should be removed - it is used in V3... not implemented here yet
    }

    /// <summary>
    /// Gets a value indicating whether the selected account has unresolved conflicts.
    /// </summary>
    public bool HasUnresolvedConflicts
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets a value indicating whether sync progress view should be shown.
    /// </summary>
    public bool ShowSyncProgress => SyncProgress is not null && ConflictResolution is null;

    /// <summary>
    /// Gets a value indicating whether conflict resolution view should be shown.
    /// </summary>
    public bool ShowConflictResolution => ConflictResolution is not null;

    /// <summary>
    /// Gets the command to open the Update Account Details window.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenUpdateAccountDetailsCommand { get; }

    /// <summary>
    /// Gets the command to open the Manage Account Details window.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenManageAccountDetailsCommand { get; }

    /// <summary>
    /// Gets the command to close the application.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CloseApplicationCommand { get; }

    /// <summary>
    /// Gets the command to open the View Sync History window.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenViewSyncHistoryCommand { get; }

    /// <summary>
    /// Gets the command to open the Debug Log Viewer window.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenDebugLogViewerCommand { get; }

    /// <summary>
    /// Gets the command to view unresolved conflicts.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ViewConflictsCommand { get; }

    /// <summary>
    /// Gets the account management view model.
    /// </summary>
    public AccountManagementViewModel AccountManagement { get; }

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

    /// <summary>
    /// Gets the conflict resolution view model (when viewing conflicts).
    /// </summary>
    public ConflictResolutionViewModel? ConflictResolution
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

#pragma warning disable S2325 // Methods and properties that don't access instance data should be static - needed for binding in MainWindow
    public string ApplicationName => ApplicationMetadata.ApplicationName;
#pragma warning restore S2325 // Methods and properties that don't access instance data should be static - needed for binding in MainWindow
    private readonly IServiceProvider _serviceProvider;
    private const int MaxRecentTransfers = 15;

    public MainWindowViewModel(ISyncCommandService syncCommandService, ISyncronisationCoordinator syncCoordinator,
      ISettingsAndPreferencesService settingsAndPreferencesService, IAuthService authService,
        AccountManagementViewModel accountManagementViewModel, ISyncConflictRepository conflictRepository, IServiceProvider serviceProvider)
    {
        AccountManagement = accountManagementViewModel;

        _conflictRepository = conflictRepository;
        _serviceProvider = serviceProvider;
        _syncCoordinator = syncCoordinator;
        UserPreferences = settingsAndPreferencesService.Load();
        SignedIn = authService.IsUserSignedInAsync("PlaceholderAccountId", CancellationToken.None).GetAwaiter().GetResult();
        IObservable<bool> isSyncing = this.WhenAnyValue(x => x.OperationType)
            .Select(type => type == SyncOperationType.Syncing);

        SignInCommand = syncCommandService.CreateSignInCommand(this);
        InitialSyncCommand = syncCommandService.CreateInitialSyncCommand(this, isSyncing);
        DeltaToken? fullSync = _syncCoordinator.GetDeltaTokenAsync("PlaceholderAccountId", CancellationToken.None).GetAwaiter().GetResult();
        IncrementalSyncCommand = syncCommandService.CreateIncrementalSyncCommand(fullSync ?? new DeltaToken("PlaceholderAccountId", string.Empty, string.Empty, DateTimeOffset.MinValue), this, isSyncing);
        CancelSyncCommand = syncCommandService.CreateCancelSyncCommand(this, isSyncing);
        ScanLocalFilesCommand = syncCommandService.CreateScanLocalFilesCommand(this, isSyncing);

        SetFullSync(fullSync == null);
        SetIncrementalSync(fullSync != null);
        SubscribeToSyncProgress();
        SubscribeToTransferProgress(_syncCoordinator);
        // Commands
        OpenUpdateAccountDetailsCommand = ReactiveCommand.Create(OpenUpdateAccountDetails);
        OpenManageAccountDetailsCommand = ReactiveCommand.Create(OpenManageAccountDetails);
        OpenViewSyncHistoryCommand = ReactiveCommand.Create(OpenViewSyncHistory);
        OpenDebugLogViewerCommand = ReactiveCommand.Create(OpenDebugLogViewer);
        ViewConflictsCommand = ReactiveCommand.Create(ViewConflicts, this.WhenAnyValue(x => x.HasUnresolvedConflicts));
        CloseApplicationCommand = ReactiveCommand.Create(CloseApplication);
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
                        AddRecentTransfer($"{progress.Timestamp:HH:mm:ss} - {progress.CurrentOperationMessage}");
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
            var downloads = await _syncCoordinator.GetPendingDownloadCountAsync("PlaceholderAccountId",CancellationToken.None);
            var uploads = await _syncCoordinator.GetPendingUploadCountAsync("PlaceholderAccountId", CancellationToken.None);
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
            RecentTransfers.RemoveAt(RecentTransfers.Count - 1);
    }

    /// <summary>
    /// Opens the conflict resolution view for the selected account.
    /// </summary>
    private void ViewConflicts()
    {
        if(AccountManagement.SelectedAccount is not null)
            ShowConflictResolutionView(AccountManagement.SelectedAccount.AccountId);
    }

    /// <summary>
    /// Opens the Update Account Details window.
    /// </summary>
    private static void OpenUpdateAccountDetails()
    {
        var window = new UpdateAccountDetailsWindow();

        if(Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
            _ = window.ShowDialog(desktop.MainWindow);
    }

    /// <summary>
    /// Opens the Update Account Details window.
    /// </summary>
    private static void OpenManageAccountDetails()
    {
        // Get the current MainWindowViewModel instance
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        if (lifetime?.MainWindow?.DataContext is MainWindowViewModel mainVm)
        {
            var window = new AccountManagementView(mainVm.AccountManagement);
            _ = window.ShowDialog(lifetime.MainWindow);
        }
    }

    /// <summary>
    /// Opens the View Sync History window.
    /// </summary>
    private static void OpenViewSyncHistory()
    {
        var window = new ViewSyncHistoryWindow();

        if(Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
            _ = window.ShowDialog(desktop.MainWindow);
    }

    /// <summary>
    /// Opens the Debug Log Viewer window.
    /// </summary>
    private static void OpenDebugLogViewer()
    {
        var window = new DebugLogWindow();

        if(Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
            _ = window.ShowDialog(desktop.MainWindow);
    }

    /// <summary>
    /// Closes the application.
    /// </summary>
    private static void CloseApplication()
    {
        if(Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    /// <summary>
    /// Shows the conflict resolution view for the specified account.
    /// </summary>
    /// <param name="accountId">The account ID to show conflicts for.</param>
    private void ShowConflictResolutionView(string accountId)
    {
        ConflictResolution?.Dispose();
        ConflictResolutionViewModel conflictResolutionVm = ActivatorUtilities.CreateInstance<ConflictResolutionViewModel>(
            _serviceProvider,
            accountId);

        // Wire up CancelCommand to return to sync progress
        _ = conflictResolutionVm.CancelCommand
            .Subscribe(_ => CloseConflictResolutionView())
            .DisposeWith(_disposables);

        // Wire up ResolveAllCommand to return to sync progress after resolution
        _ = conflictResolutionVm.ResolveAllCommand
            .Subscribe(_ =>
                // Delay closing to allow user to see the status message
                Observable.Timer(TimeSpan.FromSeconds(2))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => CloseConflictResolutionView())
                    .DisposeWith(_disposables))
            .DisposeWith(_disposables);

        ConflictResolution = conflictResolutionVm;
        this.RaisePropertyChanged(nameof(ShowSyncProgress));
        this.RaisePropertyChanged(nameof(ShowConflictResolution));
    }

    /// <summary>
    /// Closes the conflict resolution view and returns to sync progress.
    /// </summary>
#pragma warning disable S3168 // "async" methods should not return "void"
    private async void CloseConflictResolutionView()
#pragma warning restore S3168 // "async" methods should not return "void"
    {
        ConflictResolution?.Dispose();
        ConflictResolution = null;
        this.RaisePropertyChanged(nameof(ShowSyncProgress));
        this.RaisePropertyChanged(nameof(ShowConflictResolution));

        // Refresh conflict count after resolving conflicts
        if(SyncProgress is not null)
            await SyncProgress.RefreshConflictCountAsync();

        // Update main window conflict status
        if(AccountManagement.SelectedAccount is not null)
            await UpdateConflictStatusAsync(AccountManagement.SelectedAccount.AccountId);
    }

    /// <summary>
    /// Updates the conflict status indicator for the specified account.
    /// </summary>
    /// <param name="accountId">The account ID to check.</param>
    private async Task UpdateConflictStatusAsync(string accountId)
    {
        IReadOnlyList<Core.Entities.SyncConflict> conflicts = await _conflictRepository.GetUnresolvedByAccountIdAsync(accountId);
        HasUnresolvedConflicts = conflicts.Any();
    }

    public void Dispose() => _disposables?.Dispose();
}
