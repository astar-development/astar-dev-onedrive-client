using System.Reactive;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.ViewModels;

/// <summary>
/// Provides factory methods for creating sync-related ReactiveCommands.
/// </summary>
public interface ISyncCommandService
{
    /// <summary>
    /// Creates a command for the initial full sync operation.
    /// </summary>
    ReactiveCommand<Unit, Unit> CreateInitialSyncCommand(ISyncStatusTarget target, IObservable<bool> isSyncing);

    /// <summary>
    /// Creates a command for the incremental sync operation.
    /// </summary>
    ReactiveCommand<Unit, Unit> CreateIncrementalSyncCommand(ISyncStatusTarget target, IObservable<bool> isSyncing);

    /// <summary>
    /// Creates a command for scanning local files.
    /// </summary>
    ReactiveCommand<Unit, Unit> CreateScanLocalFilesCommand(ISyncStatusTarget target, IObservable<bool> isSyncing);

    /// <summary>
    /// Creates a command for signing in.
    /// </summary>
    ReactiveCommand<Unit, Unit> CreateSignInCommand(ISyncStatusTarget target);

    /// <summary>
    /// Creates a command for cancelling the current sync operation.
    /// </summary>
    ReactiveCommand<Unit, Unit> CreateCancelSyncCommand(ISyncStatusTarget target, IObservable<bool> isSyncing);
}
