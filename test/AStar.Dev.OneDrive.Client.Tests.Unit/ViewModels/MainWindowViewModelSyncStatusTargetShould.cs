using System.Reactive.Subjects;
using System.Reflection;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Services;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.Services.Syncronisation;
using AStar.Dev.OneDrive.Client.SettingsAndPreferences;
using AStar.Dev.OneDrive.Client.ViewModels;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.ViewModels;

public class MainWindowViewModelSyncStatusTargetShould
{
    private static MainWindowViewModel CreateViewModelForTarget()
    {
        ISyncCommandService syncCommandService = Substitute.For<ISyncCommandService>();
        ISyncEngine sync = Substitute.For<ISyncEngine>();
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        ITransferService transfer = Substitute.For<ITransferService>();
        ISettingsAndPreferencesService settings = Substitute.For<ISettingsAndPreferencesService>();
        IAuthService authService = Substitute.For<IAuthService>();
        settings.Load().Returns(new UserPreferences());
        sync.Progress.Returns(new Subject<SyncProgress>());
        transfer.Progress.Returns(new Subject<SyncProgress>());
        ISyncronisationCoordinator syncCoordinator = new SyncronisationCoordinator(sync, repo, transfer);
        return new MainWindowViewModel(syncCommandService, syncCoordinator, settings, authService, null!, null!, null!);
    }

    [Theory]
    [InlineData(3661, "1h 1m 1s")]
    [InlineData(61, "1m 1s")]
    [InlineData(59, "59s")]
    public void BuildElapsedShouldFormatElapsedTimeCorrectly(double seconds, string expected)
    {
        MethodInfo? method = typeof(MainWindowViewModel).GetMethod("BuildElapsed", BindingFlags.NonPublic | BindingFlags.Static);
        method.ShouldNotBeNull();
        var result = (string)method.Invoke(null, [TimeSpan.FromSeconds(seconds)])!;
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(3661, "ETA: 1h 1m")]
    [InlineData(61, "ETA: 1m 1s")]
    [InlineData(59, "ETA: 59s")]
    public void BuildEtaShouldFormatEtaCorrectly(double seconds, string expected)
    {
        MethodInfo? method = typeof(MainWindowViewModel).GetMethod("BuildEta", BindingFlags.NonPublic | BindingFlags.Static);
        method.ShouldNotBeNull();
        var result = (string)method.Invoke(null, [TimeSpan.FromSeconds(seconds)])!;
        result.ShouldBe(expected);
    }

    [Fact]
    public void SetStatusShouldUpdateSyncStatusMessage()
    {
        MainWindowViewModel vm = CreateViewModelForTarget();
        vm.SetStatus("Test status");
        vm.SyncStatusMessage.ShouldBe("Test status");
    }

    [Fact]
    public void SetProgressShouldUpdateProgressPercent()
    {
        MainWindowViewModel vm = CreateViewModelForTarget();
        vm.SetProgress(42.5);
        vm.ProgressPercent.ShouldBe(42.5);
    }

    [Fact]
    public void AddRecentTransferShouldAddMessageToRecentTransfers()
    {
        MainWindowViewModel vm = CreateViewModelForTarget();
        vm.AddRecentTransfer("Test transfer");
        vm.RecentTransfers[0].ShouldBe("Test transfer");
    }

    [Fact(Skip = "How did this get through???")]
    public async Task OnSyncCompletedShouldRefreshStats()
    {
        MainWindowViewModel vm = CreateViewModelForTarget();
        // Setup repo to return known values
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        repo.GetPendingDownloadCountAsync("PlaceholderAccountId", default).ReturnsForAnyArgs(7);
        repo.GetPendingUploadCountAsync("PlaceholderAccountId", default).ReturnsForAnyArgs(3);
        FieldInfo? field = typeof(MainWindowViewModel).GetField("_repo", BindingFlags.NonPublic | BindingFlags.Instance);
        field.ShouldNotBeNull();
        field.SetValue(vm, repo);
        vm.OnSyncCompleted();
        await Task.Delay(150);
        vm.PendingDownloads.ShouldBe(7);
        vm.PendingUploads.ShouldBe(3);
        vm.ProgressPercent.ShouldBe(100d);
    }

    [Fact]
    public void OnSyncFailedShouldSetStatusAndAddError()
    {
        MainWindowViewModel vm = CreateViewModelForTarget();
        vm.OnSyncFailed("TestOp", new InvalidOperationException("fail reason"));
        vm.SyncStatusMessage.ShouldBe("TestOp failed");
        vm.ProgressPercent.ShouldBe(0d);
        vm.RecentTransfers[0].ShouldContain("ERROR: Sync error: fail reason");
        vm.RecentTransfers[1].ShouldContain("TestOp failed");
    }

    [Fact]
    public void OnSyncCancelledShouldSetStatusAndAddCancelled()
    {
        MainWindowViewModel vm = CreateViewModelForTarget();
        vm.OnSyncCancelled("TestOp");
        vm.SyncStatusMessage.ShouldBe("TestOp cancelled");
        vm.ProgressPercent.ShouldBe(0d);
        vm.RecentTransfers[0].ShouldContain("TestOp was cancelled");
    }

    [Fact]
    public void SetSignedInShouldUpdateSignedIn()
    {
        MainWindowViewModel vm = CreateViewModelForTarget();
        vm.SetSignedIn(true);
        vm.SignedIn.ShouldBe(true);
        vm.SetSignedIn(false);
        vm.SignedIn.ShouldBe(false);
    }
}
