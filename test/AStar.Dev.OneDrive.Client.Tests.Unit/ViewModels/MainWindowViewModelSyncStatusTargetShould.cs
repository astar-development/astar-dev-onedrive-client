using System.Reactive.Subjects;
using System.Reflection;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Services;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
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
        settings.Load().Returns(new UserPreferences());
        sync.Progress.Returns(new Subject<SyncProgress>());
        transfer.Progress.Returns(new Subject<SyncProgress>());
        return new MainWindowViewModel(syncCommandService, sync, repo, transfer, settings);
    }

    [Theory]
    [InlineData(3661, "1h 1m 1s")]
    [InlineData(61, "1m 1s")]
    [InlineData(59, "59s")]
    public void BuildElapsed_FormatsCorrectly(double seconds, string expected)
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
    public void BuildEta_FormatsCorrectly(double seconds, string expected)
    {
        MethodInfo? method = typeof(MainWindowViewModel).GetMethod("BuildEta", BindingFlags.NonPublic | BindingFlags.Static);
        method.ShouldNotBeNull();
        var result = (string)method.Invoke(null, [TimeSpan.FromSeconds(seconds)])!;
        result.ShouldBe(expected);
    }

    [Fact]
    public void SetStatus_UpdatesSyncStatusMessage()
    {
        MainWindowViewModel vm = CreateViewModelForTarget();
        vm.SetStatus("Test status");
        vm.SyncStatusMessage.ShouldBe("Test status");
    }

    [Fact]
    public void SetProgress_UpdatesProgressPercent()
    {
        MainWindowViewModel vm = CreateViewModelForTarget();
        vm.SetProgress(42.5);
        vm.ProgressPercent.ShouldBe(42.5);
    }

    [Fact]
    public void AddRecentTransfer_AddsMessageToRecentTransfers()
    {
        MainWindowViewModel vm = CreateViewModelForTarget();
        vm.AddRecentTransfer("Test transfer");
        vm.RecentTransfers[0].ShouldBe("Test transfer");
    }

    [Fact]
    public async Task OnSyncCompleted_RefreshesStats()
    {
        MainWindowViewModel vm = CreateViewModelForTarget();
        // Setup repo to return known values
        ISyncRepository repo = Substitute.For<ISyncRepository>();
        repo.GetPendingDownloadCountAsync(default).ReturnsForAnyArgs(7);
        repo.GetPendingUploadCountAsync(default).ReturnsForAnyArgs(3);
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
    public void OnSyncFailed_SetsStatusAndAddsError()
    {
        MainWindowViewModel vm = CreateViewModelForTarget();
        vm.OnSyncFailed("TestOp", new InvalidOperationException("fail reason"));
        vm.SyncStatusMessage.ShouldBe("TestOp failed");
        vm.ProgressPercent.ShouldBe(0d);
        vm.RecentTransfers[0].ShouldContain("ERROR: Sync error: fail reason");
        vm.RecentTransfers[1].ShouldContain("TestOp failed");
    }

    [Fact]
    public void OnSyncCancelled_SetsStatusAndAddsCancelled()
    {
        MainWindowViewModel vm = CreateViewModelForTarget();
        vm.OnSyncCancelled("TestOp");
        vm.SyncStatusMessage.ShouldBe("TestOp cancelled");
        vm.ProgressPercent.ShouldBe(0d);
        vm.RecentTransfers[0].ShouldContain("TestOp was cancelled");
    }

    [Fact]
    public void SetSignedIn_UpdatesSignedIn()
    {
        MainWindowViewModel vm = CreateViewModelForTarget();
        vm.SetSignedIn(true);
        vm.SignedIn.ShouldBe(true);
        vm.SetSignedIn(false);
        vm.SignedIn.ShouldBe(false);
    }
}
