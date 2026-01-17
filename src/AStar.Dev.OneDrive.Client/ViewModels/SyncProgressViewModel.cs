using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.FromV3;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Models.Enums;
using Microsoft.Extensions.Logging;
using ReactiveUI;

#pragma warning disable CA1848 // Use LoggerMessage delegates

namespace AStar.Dev.OneDrive.Client.ViewModels;

/// <summary>
/// ViewModel for displaying real-time sync progress with pause/resume controls.
/// </summary>
public sealed class SyncProgressViewModel : ReactiveObject, IDisposable
{
    private readonly ISyncEngine _syncEngine;
    private readonly CompositeDisposable _disposables = [];
    private readonly ILogger<SyncProgressViewModel> _logger;

    /// <summary>
    /// Gets the account ID for this sync progress.
    /// </summary>
    public string AccountId { get; }

    /// <summary>
    /// Gets or sets the current sync progress state.
    /// </summary>
    public SyncState? CurrentProgress { get; set => this.RaiseAndSetIfChanged(ref field, value); }

    /// <summary>
    /// Gets or sets a value indicating whether sync is currently running.
    /// </summary>
    public bool IsSyncing { get; set => this.RaiseAndSetIfChanged(ref field, value); }

    /// <summary>
    /// Gets a value indicating whether the progress bar should be indeterminate.
    /// True only during scanning phase when we don't know the total file count yet.
    /// </summary>
    public bool IsProgressIndeterminate => IsSyncing && (CurrentProgress is null || CurrentProgress.TotalFiles == 0);

    /// <summary>
    /// Gets or sets the status message to display.
    /// </summary>
    public string StatusMessage { get; set => this.RaiseAndSetIfChanged(ref field, value); } = "Ready to sync";

    /// <summary>
    /// Gets the progress percentage (0-100).
    /// </summary>
    public double ProgressPercentage => CurrentProgress is null || CurrentProgress.TotalFiles == 0
                ? 0
                : (double)CurrentProgress.CompletedFiles / CurrentProgress.TotalFiles * 100;

    /// <summary>
    /// Gets a formatted string showing completed/total files.
    /// </summary>
    public string FilesProgressText => CurrentProgress is null ? "No files" : $"{CurrentProgress.CompletedFiles} of {CurrentProgress.TotalFiles} files";

    /// <summary>
    /// Gets a formatted string showing upload/download counts.
    /// </summary>
    public string TransferDetailsText
    {
        get
        {
            if(CurrentProgress is null)
            {
                return string.Empty;
            }

            var parts = new List<string>();

            // Show scanning folder if currently scanning
            if(!string.IsNullOrEmpty(CurrentProgress.CurrentScanningFolder))
            {
                parts.Add($"Scanning: {CurrentProgress.CurrentScanningFolder}");
            }
            else
            {
                // Show upload/download counts during transfer
                parts.Add($"↑ {CurrentProgress.FilesUploading} uploading  ↓ {CurrentProgress.FilesDownloading} downloading");

                // Add transfer speed if available
                if(CurrentProgress.MegabytesPerSecond > 0.01)
                {
                    parts.Add($"{CurrentProgress.MegabytesPerSecond:F2} MB/s");
                }

                // Add ETA if available
                if(CurrentProgress.EstimatedSecondsRemaining.HasValue)
                {
                    var eta = FormatTimeRemaining(CurrentProgress.EstimatedSecondsRemaining.Value);
                    parts.Add($"ETA: {eta}");
                }
            }

            return string.Join("  •  ", parts);
        }
    }

    /// <summary>
    /// Gets a formatted string showing conflicts detected.
    /// </summary>
    public string ConflictsText => CurrentProgress is null || CurrentProgress.ConflictsDetected == 0
                ? string.Empty
                : $"⚠ {CurrentProgress.ConflictsDetected} conflict(s) detected";

    /// <summary>
    /// Gets a value indicating whether conflicts were detected.
    /// </summary>
    public bool HasConflicts => CurrentProgress?.ConflictsDetected > 0;

    /// <summary>
    /// Gets the command to start sync.
    /// </summary>
    public ReactiveCommand<Unit, Unit> StartSyncCommand { get; }

    /// <summary>
    /// Gets the command to pause sync.
    /// </summary>
    public ReactiveCommand<Unit, Unit> PauseSyncCommand { get; }

    /// <summary>
    /// Gets the command to view conflicts.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ViewConflictsCommand { get; }

    /// <summary>
    /// Gets the command to close the sync progress view.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="SyncProgressViewModel"/>.
    /// </summary>
    /// <param name="accountId">The account ID to sync.</param>
    /// <param name="syncEngine">The sync engine service.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="accountId"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is <c>null</c>.</exception>
    public SyncProgressViewModel(
        string accountId,
        ISyncEngine syncEngine,
        ILogger<SyncProgressViewModel> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentNullException.ThrowIfNull(syncEngine);
        ArgumentNullException.ThrowIfNull(logger);

        AccountId = accountId;
        _syncEngine = syncEngine;
        _logger = logger;

        IObservable<bool> canStart = this.WhenAnyValue(x => x.IsSyncing, isSyncing => !isSyncing);
        IObservable<bool> canPause = this.WhenAnyValue(x => x.IsSyncing);
        IObservable<bool> hasConflicts = this.WhenAnyValue(x => x.HasConflicts);

        StartSyncCommand = ReactiveCommand.CreateFromTask(StartSyncAsync, canStart);
        PauseSyncCommand = ReactiveCommand.CreateFromTask(PauseSyncAsync, canPause);
        ViewConflictsCommand = ReactiveCommand.Create(OnViewConflicts, hasConflicts);
        CloseCommand = ReactiveCommand.Create(() => { /* Handled by MainWindowViewModel */ });

        // Subscribe to sync progress updates
        _ = _syncEngine.Progress
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(progress =>
            {
                CurrentProgress = progress;

                // Update IsSyncing based on sync status
                IsSyncing = progress.Status == SyncStatus.Running;

                UpdateStatusMessage();
                this.RaisePropertyChanged(nameof(ProgressPercentage));
                this.RaisePropertyChanged(nameof(FilesProgressText));
                this.RaisePropertyChanged(nameof(TransferDetailsText));
                this.RaisePropertyChanged(nameof(ConflictsText));
                this.RaisePropertyChanged(nameof(HasConflicts));
                this.RaisePropertyChanged(nameof(IsProgressIndeterminate));

                _logger.LogDebug(
                    "Progress update: {Completed}/{Total} files, {Conflicts} conflicts",
                    progress.CompletedFiles,
                    progress.TotalFiles,
                    progress.ConflictsDetected);
            })
            .DisposeWith(_disposables);

        _logger.LogInformation("SyncProgressViewModel initialized for account {AccountId}", accountId);
    }

    /// <summary>
    /// Starts the sync operation.
    /// </summary>
    private async Task StartSyncAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsSyncing = true;
            StatusMessage = "Starting sync...";
            _logger.LogInformation("Starting sync for account {AccountId}", AccountId);

            await _syncEngine.StartSyncAsync(AccountId, cancellationToken);

            StatusMessage = "Sync completed successfully";
            _logger.LogInformation("Sync completed for account {AccountId}", AccountId);
        }
        catch(OperationCanceledException)
        {
            StatusMessage = "Sync paused";
#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
            _logger.LogInformation("Sync paused for account {AccountId}", AccountId);
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.
        }
        catch(Exception ex)
        {
            StatusMessage = $"Sync failed: {ex.Message}";
            _logger.LogError(ex, "Sync failed for account {AccountId}", AccountId);
        }
        finally
        {
            IsSyncing = false;
        }
    }

    /// <summary>
    /// Pauses the sync operation.
    /// </summary>
    private async Task PauseSyncAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            StatusMessage = "Pausing sync...";
            _logger.LogInformation("Pausing sync for account {AccountId}", AccountId);

            await _syncEngine.StopSyncAsync();

            StatusMessage = "Sync paused";
            IsSyncing = false;
        }
        catch(Exception ex)
        {
            StatusMessage = $"Failed to pause sync: {ex.Message}";
            _logger.LogError(ex, "Failed to pause sync for account {AccountId}", AccountId);
        }
    }

    /// <summary>
    /// Handles the view conflicts command.
    /// </summary>
    private void OnViewConflicts() => _logger.LogInformation("User requested to view conflicts for account {AccountId}", AccountId);

    /// <summary>
    /// Updates the status message based on current progress.
    /// </summary>
    private void UpdateStatusMessage()
    {
        if(CurrentProgress is null)
        {
            StatusMessage = "Ready to sync";
            return;
        }

        if(IsSyncing)
        {
            if(CurrentProgress.TotalFiles == 0)
            {
                // During scanning phase, show the folder being scanned
                StatusMessage = string.IsNullOrEmpty(CurrentProgress.CurrentScanningFolder)
                    ? "Scanning for changes..."
                    : $"Scanning: {CurrentProgress.CurrentScanningFolder}";
            }
            else
            {
                StatusMessage = CurrentProgress.CompletedFiles == CurrentProgress.TotalFiles
                    ? "Finalizing sync..."
                    : $"Syncing {CurrentProgress.CompletedFiles} of {CurrentProgress.TotalFiles} files...";
            }
        }
        else if(CurrentProgress.CompletedFiles == CurrentProgress.TotalFiles && CurrentProgress.TotalFiles > 0)
        {
            StatusMessage = $"Sync completed - {CurrentProgress.TotalFiles} file(s) processed";
        }
    }

    /// <summary>
    /// Refreshes the conflict count from the database and updates CurrentProgress.
    /// </summary>
    /// <remarks>
    /// Call this method after resolving conflicts to update the UI with the current conflict count.
    /// </remarks>
    public async Task RefreshConflictCountAsync(CancellationToken cancellationToken = default)
    {
        if(CurrentProgress is null)
        {
            return;
        }

        IReadOnlyList<SyncConflict> conflicts = await _syncEngine.GetConflictsAsync(AccountId, cancellationToken);
        var conflictCount = conflicts.Count;

        CurrentProgress = CurrentProgress with
        {
            ConflictsDetected = conflictCount
        };

        this.RaisePropertyChanged(nameof(ConflictsText));
        this.RaisePropertyChanged(nameof(HasConflicts));

        _logger.LogDebug("Refreshed conflict count for account {AccountId}: {Count} conflicts", AccountId, conflictCount);
    }

    /// <summary>
    /// Formats time remaining in seconds to a human-readable string.
    /// </summary>
    /// <param name="seconds">Total seconds remaining.</param>
    /// <returns>Formatted time string (e.g., "2h 15m", "45m", "30s").</returns>
    private static string FormatTimeRemaining(int seconds)
    {
        if(seconds < 60)
        {
            return $"{seconds}s";
        }

        if(seconds < 3600)
        {
            var minutes = seconds / 60;
            return $"{minutes}m";
        }

        var hours = seconds / 3600;
        var remainingMinutes = seconds % 3600 / 60;
        return remainingMinutes > 0 ? $"{hours}h {remainingMinutes}m" : $"{hours}h";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _disposables.Dispose();
        _logger.LogDebug("SyncProgressViewModel disposed for account {AccountId}", AccountId);
    }
}
