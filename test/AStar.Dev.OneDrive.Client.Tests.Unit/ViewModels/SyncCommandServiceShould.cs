
using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Services;
using AStar.Dev.OneDrive.Client.ViewModels;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.ViewModels;

public class SyncCommandServiceShould
{
    private static (SyncCommandService service, ISyncEngine sync, IAuthService auth, ISyncStatusTarget target, ILogger<SyncCommandService> logger) Create()
    {
        ISyncEngine sync = Substitute.For<ISyncEngine>();
        IAuthService auth = Substitute.For<IAuthService>();
        ILogger<SyncCommandService> logger = Substitute.For<ILogger<SyncCommandService>>();
        ISyncStatusTarget target = Substitute.For<ISyncStatusTarget>();
        var service = new SyncCommandService(auth, sync, logger);
        return (service, sync, auth, target, logger);
    }

    [Fact]
    public async Task CreateInitialSyncCommand_HappyPath()
    {
        (SyncCommandService service, ISyncEngine sync, IAuthService _, ISyncStatusTarget target, ILogger<SyncCommandService> logger) = Create();
        sync.InitialFullSyncAsync("PlaceholderAccountId", Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> command = service.CreateInitialSyncCommand(target, Observable.Return(false));
        await command.Execute();
        target.Received().SetStatus("Running initial full sync");
        target.Received().SetProgress(0);
        target.Received().SetStatus("Initial sync complete");
        target.Received().SetProgress(100);
        target.Received().AddRecentTransfer("Initial sync completed successfully");
        target.Received().OnSyncCompleted();
        logger.Received().LogInformation("Initial sync completed successfully");
    }

    [Fact]
    public async Task CreateIncrementalSyncCommand_HappyPath()
    {
        var deltaToken = new DeltaToken("PlaceholderAccountId","deltaId", "anotherId", DateTimeOffset.MinValue);
        (SyncCommandService service, ISyncEngine sync, IAuthService _, ISyncStatusTarget target, ILogger<SyncCommandService> logger) = Create();
        sync.IncrementalSyncAsync("PlaceholderAccountId", deltaToken, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> command = service.CreateIncrementalSyncCommand(deltaToken, target, Observable.Return(false));
        await command.Execute();
        target.Received().SetStatus("Running incremental sync");
        target.Received().SetProgress(0);
        target.Received().SetStatus("Incremental sync complete");
        target.Received().SetProgress(100);
        target.Received().AddRecentTransfer("Incremental sync completed successfully");
        target.Received().OnSyncCompleted();
        logger.Received().LogInformation("Incremental sync completed successfully");
    }

    [Fact]
    public async Task CreateScanLocalFilesCommand_HappyPath()
    {
        (SyncCommandService service, ISyncEngine sync, IAuthService _, ISyncStatusTarget target, ILogger<SyncCommandService> logger) = Create();
        sync.ScanLocalFilesAsync("PlaceholderAccountId", Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> command = service.CreateScanLocalFilesCommand(target, Observable.Return(false));
        await command.Execute();
        target.Received().SetStatus("Processing local file sync...");
        target.Received().SetProgress(0);
        target.Received().SetStatus("Local file sync completed successfully");
        target.Received().SetProgress(100);
        target.Received().AddRecentTransfer("Local file sync completed successfully");
        target.Received().OnSyncCompleted();
        logger.Received().LogInformation("Local file sync completed successfully");
    }

    [Fact]
    public async Task CreateSignInCommand_HappyPath()
    {
        (SyncCommandService service, ISyncEngine _, IAuthService auth, ISyncStatusTarget target, ILogger<SyncCommandService> logger) = Create();
        auth.SignInAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> command = service.CreateSignInCommand(target);
        await command.Execute();
        target.Received().SetStatus("Signing in...");
        target.Received().SetStatus("Signed in");
        target.Received().SetSignedIn(true);
        target.Received().AddRecentTransfer(Arg.Is<string>(s => s.StartsWith("Signed in at")));
        logger.Received().LogInformation("User successfully signed in");
    }

    [Fact]
    public void CreateCancelSyncCommand_HappyPath()
    {
        (SyncCommandService? service, ISyncEngine _, IAuthService _, ISyncStatusTarget? target, ILogger<SyncCommandService> _) = Create();
        ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> command = service.CreateCancelSyncCommand(target, Observable.Return(true));
        command.Execute().Subscribe();
        target.Received().SetStatus("Cancelling...");
        target.Received().AddRecentTransfer(Arg.Is<string>(s => s.Contains("Sync cancellation requested")));
    }

    [Fact]
    public async Task CreateInitialSyncCommand_HandlesCancellation()
    {
        (SyncCommandService? service, ISyncEngine? sync, IAuthService _, ISyncStatusTarget? target, ILogger<SyncCommandService>? _) = Create();
        sync.InitialFullSyncAsync("PlaceholderAccountId", Arg.Any<CancellationToken>()).Returns(_ => throw new OperationCanceledException());
        ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> command = service.CreateInitialSyncCommand(target, Observable.Return(false));
        await command.Execute();
        target.Received().OnSyncCancelled("Initial sync");
    }

    [Fact]
    public async Task CreateInitialSyncCommand_HandlesException()
    {
        (SyncCommandService? service, ISyncEngine? sync, IAuthService _, ISyncStatusTarget? target, ILogger<SyncCommandService>? logger) = Create();
        sync.InitialFullSyncAsync("PlaceholderAccountId", Arg.Any<CancellationToken>()).Returns(_ => throw new InvalidOperationException("fail"));
        ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> command = service.CreateInitialSyncCommand(target, Observable.Return(false));
        await command.Execute();
        target.Received().OnSyncFailed("Initial sync", Arg.Any<InvalidOperationException>());
        logger.Received().LogError(Arg.Any<InvalidOperationException>(), "Initial sync failed due to configuration error");
    }

    [Fact]
    public async Task CreateIncrementalSyncCommand_HandlesCancellation()
    {
        var deltaToken = new DeltaToken("PlaceholderAccountId","deltaId", "anotherId", DateTimeOffset.MinValue);
        (SyncCommandService? service, ISyncEngine? sync, IAuthService _, ISyncStatusTarget? target, ILogger<SyncCommandService>? _) = Create();
        sync.IncrementalSyncAsync("PlaceholderAccountId", Arg.Any<DeltaToken>(), Arg.Any<CancellationToken>()).Returns(_ => throw new OperationCanceledException());
        ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> command = service.CreateIncrementalSyncCommand(deltaToken, target, Observable.Return(false));
        await command.Execute();
        target.Received().OnSyncCancelled("Incremental sync");
    }

    [Fact]
    public async Task CreateIncrementalSyncCommand_HandlesDeltaTokenException()
    {
        var deltaToken = new DeltaToken("PlaceholderAccountId","deltaId", "anotherId", DateTimeOffset.MinValue);
        (SyncCommandService? service, ISyncEngine? sync, IAuthService _, ISyncStatusTarget? target, ILogger<SyncCommandService>? logger) = Create();
        sync.IncrementalSyncAsync("PlaceholderAccountId", Arg.Any<DeltaToken>(), Arg.Any<CancellationToken>()).Returns(_ => throw new InvalidOperationException("Delta token missing"));
        ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> command = service.CreateIncrementalSyncCommand(deltaToken, target, Observable.Return(false));
        await command.Execute();
        target.Received().SetStatus("Incremental sync failed");
        target.Received().SetProgress(0);
        target.Received().AddRecentTransfer("ERROR: Must run initial sync before incremental sync");
        logger.Received().LogWarning(Arg.Any<InvalidOperationException>(), "Incremental sync attempted before initial sync");
    }

    [Fact]
    public async Task CreateIncrementalSyncCommand_HandlesOtherException()
    {
        var deltaToken = new DeltaToken("PlaceholderAccountId","deltaId", "anotherId", DateTimeOffset.MinValue);
        (SyncCommandService? service, ISyncEngine? sync, IAuthService _, ISyncStatusTarget? target, ILogger<SyncCommandService>? logger) = Create();
        sync.IncrementalSyncAsync("PlaceholderAccountId",Arg.Any<DeltaToken>(), Arg.Any<CancellationToken>()).Returns(_ => throw new InvalidOperationException("other error"));
        ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> command = service.CreateIncrementalSyncCommand(deltaToken, target, Observable.Return(false));
        await command.Execute();
        target.Received().OnSyncFailed("Incremental sync", Arg.Any<InvalidOperationException>());
        logger.Received().LogError(Arg.Any<InvalidOperationException>(), "Incremental sync failed");
    }

    [Fact]
    public async Task CreateScanLocalFilesCommand_HandlesCancellation()
    {
        (SyncCommandService? service, ISyncEngine? sync, IAuthService _, ISyncStatusTarget? target, ILogger<SyncCommandService>? _) = Create();
        sync.ScanLocalFilesAsync("PlaceholderAccountId", Arg.Any<CancellationToken>()).Returns(_ => throw new OperationCanceledException());
        ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> command = service.CreateScanLocalFilesCommand(target, Observable.Return(false));
        await command.Execute();
        target.Received().OnSyncCancelled("Local file sync");
    }

    [Fact]
    public async Task CreateScanLocalFilesCommand_HandlesException()
    {
        (SyncCommandService? service, ISyncEngine? sync, IAuthService _, ISyncStatusTarget? target, ILogger<SyncCommandService>? logger) = Create();
        sync.ScanLocalFilesAsync("PlaceholderAccountId", Arg.Any<CancellationToken>()).Returns(_ => throw new InvalidOperationException("fail"));
        ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> command = service.CreateScanLocalFilesCommand(target, Observable.Return(false));
        await command.Execute();
        target.Received().OnSyncFailed("Local file sync", Arg.Any<InvalidOperationException>());
        logger.Received().LogError(Arg.Any<InvalidOperationException>(), "Local file sync failed");
    }

    [Fact]
    public async Task CreateSignInCommand_HandlesException()
    {
        (SyncCommandService? service, ISyncEngine _, IAuthService? auth, ISyncStatusTarget? target, ILogger<SyncCommandService>? logger) = Create();
        auth.SignInAsync(Arg.Any<CancellationToken>()).Returns(_ => throw new InvalidOperationException("fail"));
        ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> command = service.CreateSignInCommand(target);
        await command.Execute();
        target.Received().SetStatus("Sign-in failed");
        target.Received().SetSignedIn(false);
        target.Received().AddRecentTransfer(Arg.Is<string>(s => s.Contains("Sign-in failed: fail")));
        logger.Received().LogError(Arg.Any<InvalidOperationException>(), "Sign-in failed");
    }
}
