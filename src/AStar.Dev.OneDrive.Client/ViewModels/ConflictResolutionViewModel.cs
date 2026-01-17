using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.FromV3;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Models.Enums;
using AStar.Dev.OneDrive.Client.FromV3.Sync;
using Microsoft.Extensions.Logging;
using ReactiveUI;

#pragma warning disable CA1848 // Use LoggerMessage delegates
#pragma warning disable CA1873 // Avoid string interpolation in logging - Acceptable for ViewModels

namespace AStar.Dev.OneDrive.Client.ViewModels;

/// <summary>
/// ViewModel for the conflict resolution UI, displaying all unresolved conflicts
/// for a specific account and allowing the user to choose resolution strategies.
/// </summary>
public sealed class ConflictResolutionViewModel : ReactiveObject, IDisposable
{
    private readonly ISyncEngine _syncEngine;
    private readonly IConflictResolver _conflictResolver;
    private readonly ILogger<ConflictResolutionViewModel> _logger;
    private readonly string _accountId;
    private readonly CompositeDisposable _disposables = [];

    /// <summary>
    /// Gets the collection of conflicts to display and resolve.
    /// </summary>
    public ObservableCollection<ConflictItemViewModel> Conflicts { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether conflicts are being loaded from the database.
    /// </summary>
    public bool IsLoading
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether conflict resolution is in progress.
    /// </summary>
    public bool IsResolving
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets or sets a status message to display to the user.
    /// </summary>
    public string StatusMessage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether there are any conflicts to display.
    /// </summary>
    public bool HasConflicts => Conflicts.Count > 0;

    /// <summary>
    /// Gets the command to load conflicts from the database.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadConflictsCommand { get; }

    /// <summary>
    /// Gets the command to resolve all conflicts with user-selected strategies.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ResolveAllCommand { get; }

    /// <summary>
    /// Gets the command to cancel conflict resolution and return to the previous view.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ConflictResolutionViewModel"/>.
    /// </summary>
    /// <param name="accountId">The account ID to load conflicts for.</param>
    /// <param name="syncEngine">The sync engine for retrieving conflicts.</param>
    /// <param name="conflictResolver">The conflict resolver service.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is <c>null</c>.</exception>
    public ConflictResolutionViewModel(
        string accountId,
        ISyncEngine syncEngine,
        IConflictResolver conflictResolver,
        ILogger<ConflictResolutionViewModel> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentNullException.ThrowIfNull(syncEngine);
        ArgumentNullException.ThrowIfNull(conflictResolver);
        ArgumentNullException.ThrowIfNull(logger);

        _accountId = accountId;
        _syncEngine = syncEngine;
        _conflictResolver = conflictResolver;
        _logger = logger;

        IObservable<bool> canResolve = this.WhenAnyValue(
            x => x.IsResolving,
            x => x.HasConflicts,
            (isResolving, hasConflicts) => !isResolving && hasConflicts);

        LoadConflictsCommand = ReactiveCommand.CreateFromTask(LoadConflictsAsync);
        ResolveAllCommand = ReactiveCommand.CreateFromTask(ResolveAllAsync, canResolve);
        CancelCommand = ReactiveCommand.Create(OnCancel);

        // Observe collection changes to update HasConflicts property
        Conflicts.CollectionChanged += (_, _) => this.RaisePropertyChanged(nameof(HasConflicts));

        // Load conflicts on initialization
        _ = LoadConflictsCommand.Execute().Subscribe().DisposeWith(_disposables);
    }

    /// <summary>
    /// Loads all unresolved conflicts for the account from the database.
    /// </summary>
    private async Task LoadConflictsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading conflicts...";
            Conflicts.Clear();

            IReadOnlyList<SyncConflict> conflicts = await _syncEngine.GetConflictsAsync(_accountId, cancellationToken);

            foreach(SyncConflict conflict in conflicts)
            {
                Conflicts.Add(new ConflictItemViewModel(conflict));
            }

            StatusMessage = Conflicts.Count > 0
                ? $"Found {Conflicts.Count} conflict(s) requiring resolution."
                : "No conflicts detected.";

            _logger.LogInformation("Loaded {Count} conflicts for account {AccountId}", Conflicts.Count, _accountId);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Failed to load conflicts for account {AccountId}", _accountId);
            StatusMessage = $"Error loading conflicts: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Resolves all conflicts using the user-selected strategies.
    /// </summary>
    private async Task ResolveAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsResolving = true;
            var totalConflicts = Conflicts.Count;
            var resolvedCount = 0;
            var skippedCount = 0;

            _logger.LogInformation("Starting resolution of {Count} conflicts for account {AccountId}", totalConflicts, _accountId);

            for(var i = Conflicts.Count - 1; i >= 0; i--)
            {
                ConflictItemViewModel conflictVm = Conflicts[i];

                if(conflictVm.SelectedStrategy == ConflictResolutionStrategy.None)
                {
                    _logger.LogDebug("Skipping conflict {FilePath} with strategy None", conflictVm.FilePath);
                    skippedCount++;
                    continue;
                }

                StatusMessage = $"Resolving {conflictVm.FilePath}... ({resolvedCount + skippedCount + 1}/{totalConflicts})";

                var conflict = new SyncConflict(
                    conflictVm.Id,
                    conflictVm.AccountId,
                    conflictVm.FilePath,
                    conflictVm.LocalModifiedUtc,
                    conflictVm.RemoteModifiedUtc,
                    conflictVm.LocalSize,
                    conflictVm.RemoteSize,
                    conflictVm.DetectedUtc,
                    conflictVm.SelectedStrategy,
                    false
                );

                await _conflictResolver.ResolveAsync(conflict, conflictVm.SelectedStrategy, cancellationToken);

                Conflicts.RemoveAt(i);
                resolvedCount++;

                _logger.LogInformation(
                    "Resolved conflict {FilePath} with strategy {Strategy}",
                    conflictVm.FilePath,
                    conflictVm.SelectedStrategy);
            }

            StatusMessage = resolvedCount > 0
                ? $"Successfully resolved {resolvedCount} conflict(s)." + (skippedCount > 0 ? $" Skipped {skippedCount}." : "")
                : $"No conflicts resolved. Skipped {skippedCount}.";

            _logger.LogInformation(
                "Conflict resolution completed: {Resolved} resolved, {Skipped} skipped",
                resolvedCount,
                skippedCount);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error during conflict resolution for account {AccountId}", _accountId);
            StatusMessage = $"Error resolving conflicts: {ex.Message}";
        }
        finally
        {
            IsResolving = false;
        }
    }

    /// <summary>
    /// Handles the cancel command by clearing the status message.
    /// </summary>
    private void OnCancel()
    {
        _logger.LogInformation("User cancelled conflict resolution for account {AccountId}", _accountId);
        StatusMessage = "Conflict resolution cancelled.";
        // Navigation logic would go here in a real implementation
    }

    /// <inheritdoc/>
    public void Dispose() => _disposables.Dispose();
}
