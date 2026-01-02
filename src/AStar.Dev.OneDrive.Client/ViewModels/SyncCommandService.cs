using System.Reactive;
using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Services;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.ViewModels;

/// <inheritdoc/>
public class SyncCommandService(IAuthService auth, ISyncEngine sync, ILogger<SyncCommandService> logger) : ISyncCommandService
{
    private CancellationTokenSource? _currentSyncCancellation;

    public ReactiveCommand<Unit, Unit> CreateInitialSyncCommand(ISyncStatusTarget target, IObservable<bool> isSyncing)
        => ReactiveCommand.CreateFromTask(async ct =>
        {
            _currentSyncCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
            try
            {
                target.SetStatus("Running initial full sync");
                target.SetProgress(0);
                await sync.InitialFullSyncAsync(_currentSyncCancellation.Token);
                target.SetStatus("Initial sync complete");
                target.SetProgress(100);
                target.AddRecentTransfer("Initial sync completed successfully");
                target.OnSyncCompleted();
                logger.LogInformation("Initial sync completed successfully");
            }
            catch(OperationCanceledException)
            {
                target.OnSyncCancelled("Initial sync");
            }
            catch(InvalidOperationException ex)
            {
                target.OnSyncFailed("Initial sync", ex);
                logger.LogError(ex, "Initial sync failed due to configuration error");
            }
            catch(Exception ex)
            {
                target.OnSyncFailed("Initial sync", ex);
                logger.LogError(ex, "Initial sync failed");
            }
            finally
            {
                _currentSyncCancellation?.Dispose();
                _currentSyncCancellation = null;
            }
        }, isSyncing.Select(syncing => !syncing));

    public ReactiveCommand<Unit, Unit> CreateIncrementalSyncCommand(DeltaToken deltaToken, ISyncStatusTarget target, IObservable<bool> isSyncing)
        => ReactiveCommand.CreateFromTask(async ct =>
        {
            _currentSyncCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
            try
            {
                target.SetStatus("Running incremental sync");
                target.SetProgress(0);
                await sync.IncrementalSyncAsync(deltaToken, _currentSyncCancellation.Token);
                target.SetStatus("Incremental sync complete");
                target.SetProgress(100);
                target.AddRecentTransfer("Incremental sync completed successfully");
                target.OnSyncCompleted();
                logger.LogInformation("Incremental sync completed successfully");
            }
            catch(OperationCanceledException)
            {
                target.OnSyncCancelled("Incremental sync");
            }
            catch(InvalidOperationException ex) when(ex.Message.Contains("Delta token missing"))
            {
                target.SetStatus("Incremental sync failed");
                target.SetProgress(0);
                var errorMsg = "Must run initial sync before incremental sync";
                target.AddRecentTransfer($"ERROR: {errorMsg}");
                logger.LogWarning(ex, "Incremental sync attempted before initial sync");
            }
            catch(Exception ex)
            {
                target.OnSyncFailed("Incremental sync", ex);
                logger.LogError(ex, "Incremental sync failed");
            }
            finally
            {
                _currentSyncCancellation?.Dispose();
                _currentSyncCancellation = null;
            }
        }, isSyncing.Select(syncing => !syncing));

    public ReactiveCommand<Unit, Unit> CreateScanLocalFilesCommand(ISyncStatusTarget target, IObservable<bool> isSyncing)
        => ReactiveCommand.CreateFromTask(async ct =>
        {
            _currentSyncCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
            try
            {
                target.SetStatus("Processing local file sync...");
                target.SetProgress(0);
                target.AddRecentTransfer("Processing local file sync...");
                await sync.ScanLocalFilesAsync(_currentSyncCancellation.Token);
                target.SetStatus("Local file sync completed successfully");
                target.SetProgress(100);
                target.AddRecentTransfer("Local file sync completed successfully");
                target.OnSyncCompleted();
                logger.LogInformation("Local file sync completed successfully");
            }
            catch(OperationCanceledException)
            {
                target.OnSyncCancelled("Local file sync");
            }
            catch(Exception ex)
            {
                target.OnSyncFailed("Local file sync", ex);
                logger.LogError(ex, "Local file sync failed");
            }
            finally
            {
                _currentSyncCancellation?.Dispose();
                _currentSyncCancellation = null;
            }
        }, isSyncing.Select(syncing => !syncing));

    public ReactiveCommand<Unit, Unit> CreateSignInCommand(ISyncStatusTarget target)
        => ReactiveCommand.CreateFromTask(async ct =>
        {
            try
            {
                target.SetStatus("Signing in...");
                await auth.SignInAsync(ct);
                target.SetStatus("Signed in");
                target.SetSignedIn(true);
                target.AddRecentTransfer($"Signed in at {DateTimeOffset.Now}");
                logger.LogInformation("User successfully signed in");

                DeltaToken? token = sync.GetDeltaTokenAsync(CancellationToken.None).GetAwaiter().GetResult() ?? throw new InvalidOperationException("Delta token missing; run initial sync first.");
                if(token != null)
                {
                    target.SetFullSync(false);
                    target.SetIncrementalSync(true);
                }
                else
                {
                    target.SetFullSync(true);
                    target.SetIncrementalSync(false);
                }
            }
            catch(Exception ex)
            {
                target.SetStatus("Sign-in failed");
                target.SetSignedIn(false);
                var errorMsg = $"Sign-in failed: {ex.Message}";
                target.AddRecentTransfer($"{DateTimeOffset.Now:HH:mm:ss} - ERROR: {errorMsg}");
                logger.LogError(ex, "Sign-in failed");
            }
        });

    public ReactiveCommand<Unit, Unit> CreateCancelSyncCommand(ISyncStatusTarget target, IObservable<bool> isSyncing)
        => ReactiveCommand.Create(() =>
        {
            _currentSyncCancellation?.Cancel();
            target.SetStatus("Cancelling...");
            target.AddRecentTransfer($"{DateTimeOffset.Now:HH:mm:ss} - Sync cancellation requested");
        }, isSyncing);
}
