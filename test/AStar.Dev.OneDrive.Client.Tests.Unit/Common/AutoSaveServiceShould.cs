using AStar.Dev.OneDrive.Client.Common;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Services;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.SettingsAndPreferences;
using AStar.Dev.OneDrive.Client.ViewModels;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System.Reactive.Subjects;
using Xunit;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Common;

public class AutoSaveServiceShould
{
    [Fact]
    public void InvokeSaveActionWhenSyncStatusPropertyChanges()
    {
        var sut = new AutoSaveService();
        MainWindowViewModel viewModel = CreateViewModel();
        var saveActionInvoked = false;
        void saveAction()
        {
            saveActionInvoked = true;
        }

        sut.MonitorForChanges(viewModel, saveAction);
        viewModel.SyncStatus = "New Status";

        saveActionInvoked.ShouldBeTrue();
    }

    [Fact]
    public void NotInvokeSaveActionWhenOtherPropertiesChange()
    {
        var sut = new AutoSaveService();
        MainWindowViewModel viewModel = CreateViewModel();
        var saveActionInvoked = false;
        void saveAction()
        {
            saveActionInvoked = true;
        }

        sut.MonitorForChanges(viewModel, saveAction);
        viewModel.PendingDownloads = 5;
        viewModel.ProgressPercent = 50.0;
        viewModel.SignedIn = true;

        saveActionInvoked.ShouldBeFalse();
    }

    [Fact]
    public void InvokeSaveActionMultipleTimesWhenSyncStatusChangesMultipleTimes()
    {
        var sut = new AutoSaveService();
        MainWindowViewModel viewModel = CreateViewModel();
        var saveActionInvokeCount = 0;
        void saveAction()
        {
            saveActionInvokeCount++;
        }

        sut.MonitorForChanges(viewModel, saveAction);
        viewModel.SyncStatus = "Status 1";
        viewModel.SyncStatus = "Status 2";
        viewModel.SyncStatus = "Status 3";

        saveActionInvokeCount.ShouldBe(3);
    }

    [Fact]
    public void StopInvokingSaveActionAfterStopMonitoringIsCalled()
    {
        var sut = new AutoSaveService();
        MainWindowViewModel viewModel = CreateViewModel();
        var saveActionInvokeCount = 0;
        void saveAction()
        {
            saveActionInvokeCount++;
        }

        sut.MonitorForChanges(viewModel, saveAction);
        viewModel.SyncStatus = "Status 1";

        sut.StopMonitoring(viewModel);
        viewModel.SyncStatus = "Status 2";
        viewModel.SyncStatus = "Status 3";

        saveActionInvokeCount.ShouldBe(1);
    }

    [Fact]
    public void HandleStopMonitoringCalledWithoutPriorMonitoring()
    {
        var sut = new AutoSaveService();
        MainWindowViewModel viewModel = CreateViewModel();

        Exception exception = Record.Exception(() => sut.StopMonitoring(viewModel));

        exception.ShouldBeNull();
    }

    [Fact]
    public void HandleMultipleStopMonitoringCalls()
    {
        var sut = new AutoSaveService();
        MainWindowViewModel viewModel = CreateViewModel();
        var saveActionInvokeCount = 0;
        void saveAction()
        {
            saveActionInvokeCount++;
        }

        sut.MonitorForChanges(viewModel, saveAction);
        sut.StopMonitoring(viewModel);
        sut.StopMonitoring(viewModel);

        viewModel.SyncStatus = "New Status";

        saveActionInvokeCount.ShouldBe(0);
    }

    [Fact]
    public void AllowRestartingMonitoringAfterStopping()
    {
        var sut = new AutoSaveService();
        MainWindowViewModel viewModel = CreateViewModel();
        var saveActionInvokeCount = 0;
        void saveAction()
        {
            saveActionInvokeCount++;
        }

        sut.MonitorForChanges(viewModel, saveAction);
        viewModel.SyncStatus = "Status 1";

        sut.StopMonitoring(viewModel);
        viewModel.SyncStatus = "Status 2";

        sut.MonitorForChanges(viewModel, saveAction);
        viewModel.SyncStatus = "Status 3";

        saveActionInvokeCount.ShouldBe(2);
    }

    private static MainWindowViewModel CreateViewModel()
    {
        IAuthService auth = Substitute.For<IAuthService>();
        ISyncEngine sync = Substitute.For<ISyncEngine>();
        ITransferService transfer = Substitute.For<ITransferService>();
        ISettingsAndPreferencesService settings = Substitute.For<ISettingsAndPreferencesService>();
        ILogger<MainWindowViewModel> logger = Substitute.For<ILogger<MainWindowViewModel>>();

        settings.Load().Returns(new UserPreferences());

        sync.Progress.Returns(new Subject<SyncProgress>());
        transfer.Progress.Returns(new Subject<SyncProgress>());

        return new MainWindowViewModel(auth, sync, transfer, settings, logger);
    }
}
