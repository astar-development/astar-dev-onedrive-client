
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.FromV3;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Models.Enums;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.ViewModels;

/// <summary>
/// ViewModel for the sync tree view, managing folder hierarchy and selection.
/// </summary>
public sealed class SyncTreeViewModel : ReactiveObject, IDisposable
{
    /// <summary>
    /// Gets the estimated seconds remaining for sync (ETA).
    /// </summary>
    public double? EstimatedSecondsRemaining => SyncState.EstimatedSecondsRemaining;

    /// <summary>
    /// Gets the current sync speed in megabytes per second.
    /// </summary>
    public double MegabytesPerSecond => SyncState.MegabytesPerSecond;
    /// <summary>
    /// Gets or sets a value indicating whether the SyncProgressView is open.
    /// </summary>
    public bool IsSyncProgressOpen
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Command to open the SyncProgressView.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenSyncProgressCommand { get; }
    private readonly IFolderTreeService _folderTreeService;
    private readonly ISyncSelectionService _selectionService;
    private readonly ISyncEngine _syncEngine;
    private readonly CompositeDisposable _disposables = [];

    /// <summary>
    /// Gets or sets the currently selected account ID.
    /// </summary>
    public string? SelectedAccountId
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets the root-level folders for the selected account.
    /// </summary>
    public ObservableCollection<OneDriveFolderNode> RootFolders
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    /// <summary>
    /// Gets a value indicating whether folders are currently being loaded.
    /// </summary>
    public bool IsLoading
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets the error message if loading fails.
    /// </summary>
    public string? ErrorMessage
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets the current sync state.
    /// </summary>
    public SyncState SyncState
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = new(
        AccountId: string.Empty,
        Status: SyncStatus.Idle,
        TotalFiles: 0,
        CompletedFiles: 0,
        TotalBytes: 0,
        CompletedBytes: 0,
        FilesDownloading: 0,
        FilesUploading: 0,
        FilesDeleted: 0,
        ConflictsDetected: 0,
        MegabytesPerSecond: 0,
        EstimatedSecondsRemaining: null,
        CurrentScanningFolder: null,
        LastUpdateUtc: null);

    /// <summary>
    /// Gets a value indicating whether sync is currently running.
    /// </summary>
    public bool IsSyncing => SyncState.Status == SyncStatus.Running;

    /// <summary>
    /// Gets a value indicating whether sync is paused.
    /// </summary>
    public bool IsPaused => SyncState.Status == SyncStatus.Paused;

    /// <summary>
    /// Gets sync progress as percentage (0-100).
    /// </summary>
    public double ProgressPercentage => SyncState.TotalFiles > 0
        ? (double)SyncState.CompletedFiles / SyncState.TotalFiles * 100
        : 0;

    /// <summary>
    /// Gets sync progress text for display.
    /// </summary>
    public string ProgressText => SyncState.Status switch
    {
        SyncStatus.Idle => "Ready to sync",
        SyncStatus.Running when SyncState.TotalFiles == 0 => "Scanning for changes...",
        SyncStatus.Running => $"Syncing {SyncState.CompletedFiles} of {SyncState.TotalFiles} files",
        SyncStatus.Completed when SyncState.TotalFiles == 0 => "No changes to sync",
        SyncStatus.Completed => $"Sync completed - {SyncState.TotalFiles} files",
        SyncStatus.Paused => "Sync paused",
        SyncStatus.Failed => "Sync failed",
        SyncStatus.Queued => throw new NotImplementedException(),
        _ => string.Empty
    };

    /// <summary>
    /// Gets the result message from the last sync operation.
    /// </summary>
    public string? LastSyncResult
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Command to load root folders for the selected account.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadFoldersCommand { get; }

    /// <summary>
    /// Command to load child folders when a node is expanded.
    /// </summary>
    public ReactiveCommand<OneDriveFolderNode, Unit> LoadChildrenCommand { get; }

    /// <summary>
    /// Command to toggle folder selection state.
    /// </summary>
    public ReactiveCommand<OneDriveFolderNode, Unit> ToggleSelectionCommand { get; }

    /// <summary>
    /// Command to clear all selections.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearSelectionsCommand { get; }

    /// <summary>
    /// Command to start synchronization.
    /// </summary>
    public ReactiveCommand<Unit, Unit> StartSyncCommand { get; }

    /// <summary>
    /// Command to cancel synchronization.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelSyncCommand { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncTreeViewModel"/> class.
    /// </summary>
    /// <param name="folderTreeService">Service for loading folder hierarchies.</param>
    /// <param name="selectionService">Service for managing selection state.</param>
    /// <param name="syncEngine">Service for file synchronization.</param>
    public SyncTreeViewModel(
        IFolderTreeService folderTreeService,
        ISyncSelectionService selectionService,
        ISyncEngine syncEngine)
    {
        _folderTreeService = folderTreeService ?? throw new ArgumentNullException(nameof(folderTreeService));
        _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
        _syncEngine = syncEngine ?? throw new ArgumentNullException(nameof(syncEngine));

        LoadFoldersCommand = ReactiveCommand.CreateFromTask(LoadFoldersAsync);
        LoadChildrenCommand = ReactiveCommand.CreateFromTask<OneDriveFolderNode>(LoadChildrenAsync);
        ToggleSelectionCommand = ReactiveCommand.Create<OneDriveFolderNode>(ToggleSelection);
        ClearSelectionsCommand = ReactiveCommand.Create(ClearSelections);
        StartSyncCommand = ReactiveCommand.CreateFromTask(StartSyncAsync,
            this.WhenAnyValue(x => x.SelectedAccountId, x => x.IsSyncing,
                (accountId, isSyncing) => !string.IsNullOrEmpty(accountId) && !isSyncing));
        CancelSyncCommand = ReactiveCommand.CreateFromTask(CancelSyncAsync,
            this.WhenAnyValue(x => x.IsSyncing));

        // Only allow opening if syncing and not already open
        OpenSyncProgressCommand = ReactiveCommand.Create(
            () => { IsSyncProgressOpen = true; },
            this.WhenAnyValue(x => x.IsSyncing, x => x.IsSyncProgressOpen, (syncing, open) => syncing && !open));

        _ = this.WhenAnyValue(x => x.SelectedAccountId)
            .Subscribe(_ => LoadFoldersCommand.Execute().Subscribe())
            .DisposeWith(_disposables);

        // Subscribe to sync progress updates
        _ = _syncEngine.Progress
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(state =>
            {
                SyncState = state;
                this.RaisePropertyChanged(nameof(IsSyncing));
                this.RaisePropertyChanged(nameof(IsPaused));
                this.RaisePropertyChanged(nameof(ProgressPercentage));
                this.RaisePropertyChanged(nameof(ProgressText));
            })
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Loads root folders for the currently selected account.
    /// </summary>
    private async Task LoadFoldersAsync(CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrEmpty(SelectedAccountId))
        {
            RootFolders.Clear();
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            IReadOnlyList<OneDriveFolderNode> folders = await _folderTreeService.GetRootFoldersAsync(SelectedAccountId, cancellationToken);

            // Convert to list to ensure we work with the same object references
            var folderList = folders.ToList();

            // Apply saved selections from database BEFORE adding to observable collection - NO this relies on reference to folderList - it should return the updated list
            await _selectionService.LoadSelectionsFromDatabaseAsync(SelectedAccountId, folderList, cancellationToken);

            // Now add folders to UI with correct selection states already applied
            RootFolders.Clear();
            foreach(OneDriveFolderNode? folder in folderList)
            {
                RootFolders.Add(folder);
            }
        }
        catch(Exception ex)
        {
            ErrorMessage = $"Failed to load folders: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads child folders for a specific folder node.
    /// </summary>
    /// <param name="folder">The folder whose children should be loaded.</param>
    private async Task LoadChildrenAsync(OneDriveFolderNode folder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(folder);

        if(folder.ChildrenLoaded || string.IsNullOrEmpty(SelectedAccountId))
        {
            return;
        }

        try
        {
            folder.IsLoading = true;

            // Clear placeholder dummy child
            folder.Children.Clear();

            IReadOnlyList<OneDriveFolderNode> children = await _folderTreeService.GetChildFoldersAsync(SelectedAccountId, folder.Id, folder.IsSelected, cancellationToken);
            foreach(OneDriveFolderNode child in children)
            {
                folder.Children.Add(child);
            }

            // Apply saved selections from database to the newly loaded children
            // This ensures that sub-folder selections persist correctly
            await _selectionService.LoadSelectionsFromDatabaseAsync(SelectedAccountId, [.. folder.Children], cancellationToken);

            // After applying database selections, recalculate parent state
            // This will set parent to indeterminate if some (but not all) children are selected
            _selectionService.UpdateParentState(folder);

            folder.ChildrenLoaded = true;
        }
        catch(Exception ex)
        {
            ErrorMessage = $"Failed to load child folders: {ex.Message}";
        }
        finally
        {
            folder.IsLoading = false;
        }
    }

    /// <summary>
    /// Toggles the selection state of a folder.
    /// </summary>
    /// <param name="folder">The folder to toggle.</param>
    private void ToggleSelection(OneDriveFolderNode folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        var newState = folder.SelectionState switch
        {
            SelectionState.Unchecked => true,
            SelectionState.Checked => false,
            SelectionState.Indeterminate => true,
            _ => true
        };

        _selectionService.SetSelection(folder, newState);
        _selectionService.UpdateParentStates(folder, [.. RootFolders]);

        // Save selections to database (fire and forget for UI responsiveness)
        if(!string.IsNullOrEmpty(SelectedAccountId))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _selectionService.SaveSelectionsToDatabaseAsync(SelectedAccountId, [.. RootFolders]);
                }
                catch
                {
                    // Silently ignore persistence errors to avoid disrupting UI
                }
            });
        }
    }

    /// <summary>
    /// Clears all folder selections.
    /// </summary>
    private void ClearSelections()
    {
        _selectionService.ClearAllSelections([.. RootFolders]);

        // Clear selections from database (fire and forget for UI responsiveness)
        if(!string.IsNullOrEmpty(SelectedAccountId))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _selectionService.SaveSelectionsToDatabaseAsync(SelectedAccountId, [.. RootFolders]);
                }
                catch
                {
                    // Silently ignore persistence errors to avoid disrupting UI
                }
            });
        }
    }

    /// <summary>
    /// Gets all selected folders.
    /// </summary>
    /// <returns>List of selected folder nodes.</returns>
    public List<OneDriveFolderNode> GetSelectedFolders() => _selectionService.GetSelectedFolders([.. RootFolders]);

    /// <summary>
    /// Starts file synchronization for the selected account.
    /// </summary>
    private async Task StartSyncAsync()
    {
        if(string.IsNullOrEmpty(SelectedAccountId))
        {
            return;
        }

        try
        {
            LastSyncResult = null; // Clear previous result
            await _syncEngine.StartSyncAsync(SelectedAccountId);

            // Display result after sync completes
            if(SyncState.Status == SyncStatus.Completed)
            {
                var totalChanges = SyncState.TotalFiles + SyncState.FilesDeleted;

                LastSyncResult = totalChanges == 0 ? "✓ Sync complete: No changes detected" :
                totalChanges > 1 ? $"✓ Sync complete: {totalChanges} change(s) synchronized" : $"✓ Sync complete: {totalChanges} change synchronized";
            }
        }
        catch(OperationCanceledException)
        {
            // Sync was cancelled - this is expected, don't show error
            // The cancel command already set the LastSyncResult message
        }
        catch(Exception ex)
        {
            ErrorMessage = $"Sync failed: {ex.Message}";
            LastSyncResult = null;
        }
    }

    /// <summary>
    /// Cancels the ongoing synchronization.
    /// </summary>
    private async Task CancelSyncAsync()
    {
        await _syncEngine.StopSyncAsync();
        LastSyncResult = "Sync cancelled";
    }

    /// <inheritdoc/>
    public void Dispose() => _disposables.Dispose();
}
