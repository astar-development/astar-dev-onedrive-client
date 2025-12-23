using System.Collections.ObjectModel;
using AStar.Dev.OneDrive.Client.ViewModels;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.ViewModels;

public class DashboardViewModelShould
{
    [Fact]
    public void InitializeWithEmptyRecentTransfers()
    {
        var sut = new DashboardViewModel();

        sut.RecentTransfers.ShouldBeEmpty();
    }

    [Fact]
    public void InitializeWithZeroPendingDownloads()
    {
        var sut = new DashboardViewModel();

        sut.PendingDownloads.ShouldBe(0);
    }

    [Fact]
    public void InitializeWithZeroPendingUploads()
    {
        var sut = new DashboardViewModel();

        sut.PendingUploads.ShouldBe(0);
    }

    [Fact]
    public void InitializeWithIdleSyncStatus()
    {
        var sut = new DashboardViewModel();

        sut.SyncStatus.ShouldBe("Idle");
    }

    [Fact]
    public void UpdatePendingDownloadsValue()
    {
        var sut = new DashboardViewModel
        {
            PendingDownloads = 42
        };

        sut.PendingDownloads.ShouldBe(42);
    }

    [Fact]
    public void UpdatePendingUploadsValue()
    {
        var sut = new DashboardViewModel
        {
            PendingUploads = 15
        };

        sut.PendingUploads.ShouldBe(15);
    }

    [Fact]
    public void UpdateSyncStatusValue()
    {
        var sut = new DashboardViewModel
        {
            SyncStatus = "Syncing"
        };

        sut.SyncStatus.ShouldBe("Syncing");
    }

    [Fact]
    public void RaisePropertyChangedWhenPendingDownloadsChanges()
    {
        var sut = new DashboardViewModel();
        var propertyChanged = false;
        sut.PropertyChanged += (_, e) =>
        {
            if(e.PropertyName == nameof(DashboardViewModel.PendingDownloads))
                propertyChanged = true;
        };

        sut.PendingDownloads = 10;

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedWhenPendingUploadsChanges()
    {
        var sut = new DashboardViewModel();
        var propertyChanged = false;
        sut.PropertyChanged += (_, e) =>
        {
            if(e.PropertyName == nameof(DashboardViewModel.PendingUploads))
                propertyChanged = true;
        };

        sut.PendingUploads = 20;

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedWhenSyncStatusChanges()
    {
        var sut = new DashboardViewModel();
        var propertyChanged = false;
        sut.PropertyChanged += (_, e) =>
        {
            if(e.PropertyName == nameof(DashboardViewModel.SyncStatus))
                propertyChanged = true;
        };

        sut.SyncStatus = "Complete";

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void NotRaisePropertyChangedWhenPendingDownloadsSetToSameValue()
    {
        var sut = new DashboardViewModel
        {
            PendingDownloads = 5
        };
        var changeCount = 0;
        sut.PropertyChanged += (_, e) =>
        {
            if(e.PropertyName == nameof(DashboardViewModel.PendingDownloads))
                changeCount++;
        };

        sut.PendingDownloads = 5;

        changeCount.ShouldBe(0);
    }

    [Fact]
    public void NotRaisePropertyChangedWhenPendingUploadsSetToSameValue()
    {
        var sut = new DashboardViewModel
        {
            PendingUploads = 7
        };
        var changeCount = 0;
        sut.PropertyChanged += (_, e) =>
        {
            if(e.PropertyName == nameof(DashboardViewModel.PendingUploads))
                changeCount++;
        };

        sut.PendingUploads = 7;

        changeCount.ShouldBe(0);
    }

    [Fact]
    public void NotRaisePropertyChangedWhenSyncStatusSetToSameValue()
    {
        var sut = new DashboardViewModel
        {
            SyncStatus = "Idle"
        };
        var changeCount = 0;
        sut.PropertyChanged += (_, e) =>
        {
            if(e.PropertyName == nameof(DashboardViewModel.SyncStatus))
                changeCount++;
        };

        sut.SyncStatus = "Idle";

        changeCount.ShouldBe(0);
    }

    [Fact]
    public void AllowAddingItemsToRecentTransfers()
    {
        var sut = new DashboardViewModel();

        sut.RecentTransfers.Add("Transfer 1");
        sut.RecentTransfers.Add("Transfer 2");

        sut.RecentTransfers.Count.ShouldBe(2);
        sut.RecentTransfers[0].ShouldBe("Transfer 1");
        sut.RecentTransfers[1].ShouldBe("Transfer 2");
    }

    [Fact]
    public void AllowRemovingItemsFromRecentTransfers()
    {
        var sut = new DashboardViewModel();
        sut.RecentTransfers.Add("Transfer 1");
        sut.RecentTransfers.Add("Transfer 2");

        sut.RecentTransfers.RemoveAt(0);

        sut.RecentTransfers.Count.ShouldBe(1);
        sut.RecentTransfers[0].ShouldBe("Transfer 2");
    }

    [Fact]
    public void AllowClearingRecentTransfers()
    {
        var sut = new DashboardViewModel();
        sut.RecentTransfers.Add("Transfer 1");
        sut.RecentTransfers.Add("Transfer 2");

        sut.RecentTransfers.Clear();

        sut.RecentTransfers.ShouldBeEmpty();
    }

    [Fact]
    public void HandleMultiplePropertyChanges()
    {
        var sut = new DashboardViewModel
        {
            PendingDownloads = 10,
            PendingUploads = 5,
            SyncStatus = "Active"
        };

        sut.PendingDownloads.ShouldBe(10);
        sut.PendingUploads.ShouldBe(5);
        sut.SyncStatus.ShouldBe("Active");
    }

    [Fact]
    public void HandleNegativeValuesForPendingDownloads()
    {
        var sut = new DashboardViewModel
        {
            PendingDownloads = -1
        };

        sut.PendingDownloads.ShouldBe(-1);
    }

    [Fact]
    public void HandleNegativeValuesForPendingUploads()
    {
        var sut = new DashboardViewModel
        {
            PendingUploads = -5
        };

        sut.PendingUploads.ShouldBe(-5);
    }

    [Fact]
    public void HandleEmptyStringSyncStatus()
    {
        var sut = new DashboardViewModel
        {
            SyncStatus = string.Empty
        };

        sut.SyncStatus.ShouldBe(string.Empty);
    }

    [Fact]
    public void HandleLargeValuesForPendingDownloads()
    {
        var sut = new DashboardViewModel
        {
            PendingDownloads = int.MaxValue
        };

        sut.PendingDownloads.ShouldBe(int.MaxValue);
    }

    [Fact]
    public void HandleLargeValuesForPendingUploads()
    {
        var sut = new DashboardViewModel
        {
            PendingUploads = int.MaxValue
        };

        sut.PendingUploads.ShouldBe(int.MaxValue);
    }

    [Fact]
    public void MaintainRecentTransfersCollectionReference()
    {
        var sut = new DashboardViewModel();
        ObservableCollection<string> originalCollection = sut.RecentTransfers;

        sut.RecentTransfers.Add("Item");

        ReferenceEquals(sut.RecentTransfers, originalCollection).ShouldBeTrue();
    }
}
