using System.Collections.ObjectModel;
using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.FromV3;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Repositories;
using AStar.Dev.OneDrive.Client.ViewModels;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.FromV3.ViewModels;

public class UpdateAccountDetailsViewModelShould
{
    [Fact]
    public void ThrowArgumentNullExceptionWhenAccountRepositoryIsNull()
    {
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        ArgumentNullException exception = Should.Throw<ArgumentNullException>(() =>
            new UpdateAccountDetailsViewModel(null!, mockScheduler));

        exception.ParamName.ShouldBe("accountRepository");
    }

    [Fact]
    public void InitializeWithEmptyAccountsCollection()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        _ = sut.Accounts.ShouldBeOfType<ObservableCollection<AccountInfo>>();
        sut.Accounts.ShouldBeEmpty();
    }

    [Fact]
    public void InitializeWithNullSelectedAccount()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.SelectedAccount.ShouldBeNull();
    }

    [Fact]
    public void InitializeWithEmptyLocalSyncPath()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.LocalSyncPath.ShouldBeEmpty();
    }

    [Fact]
    public void InitializeWithEnableDetailedSyncLoggingFalse()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.EnableDetailedSyncLogging.ShouldBeFalse();
    }

    [Fact]
    public void InitializeWithEnableDebugLoggingFalse()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.EnableDebugLogging.ShouldBeFalse();
    }

    [Fact]
    public void InitializeWithEmptyStatusMessage()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.StatusMessage.ShouldBeEmpty();
    }

    [Fact]
    public void InitializeWithIsSuccessFalse()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void InitializeUpdateCommand()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        _ = sut.UpdateCommand.ShouldNotBeNull();
    }

    [Fact]
    public void InitializeCancelCommand()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        _ = sut.CancelCommand.ShouldNotBeNull();
    }

    [Fact]
    public void InitializeBrowseFolderCommand()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        _ = sut.BrowseFolderCommand.ShouldNotBeNull();
    }

    [Fact]
    public void LoadEditableFieldsWhenAccountIsSelected()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        var account = new AccountInfo(
            "account-123",
            "Test User",
            @"C:\TestPath",
            true,
            DateTime.UtcNow,
            "delta-token",
            true,
            false,
            3,
            50,
            null);

        sut.SelectedAccount = account;

        sut.LocalSyncPath.ShouldBe(@"C:\TestPath");
        sut.EnableDetailedSyncLogging.ShouldBeTrue();
        sut.StatusMessage.ShouldBeEmpty();
    }

    [Fact]
    public void LoadEnableDebugLoggingWhenAccountIsSelected()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        var account = new AccountInfo(
            "account-123",
            "Test User",
            @"C:\TestPath",
            true,
            DateTime.UtcNow,
            "delta-token",
            false,
            true,
            3,
            50,
            null);

        sut.SelectedAccount = account;

        sut.EnableDebugLogging.ShouldBeTrue();
    }

    [Fact]
    public void ClearStatusMessageWhenAccountIsSelected()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
        {
            StatusMessage = "Previous message"
        };

        var account = new AccountInfo(
            "account-123",
            "Test User",
            @"C:\TestPath",
            true,
            DateTime.UtcNow,
            null,
            false,
            false,
            3,
            50,
            null);

        sut.SelectedAccount = account;

        sut.StatusMessage.ShouldBeEmpty();
    }

    [Fact]
    public void RaisePropertyChangedWhenSelectedAccountChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(UpdateAccountDetailsViewModel.SelectedAccount))
            {
                propertyChanged = true;
            }
        };

        var account = new AccountInfo(
            "account-123",
            "Test User",
            @"C:\TestPath",
            true,
            DateTime.UtcNow,
            null,
            false,
            false,
            3,
            50,
            null);

        sut.SelectedAccount = account;

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedWhenLocalSyncPathChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(UpdateAccountDetailsViewModel.LocalSyncPath))
            {
                propertyChanged = true;
            }
        };

        sut.LocalSyncPath = @"C:\NewPath";

        propertyChanged.ShouldBeTrue();
        sut.LocalSyncPath.ShouldBe(@"C:\NewPath");
    }

    [Fact]
    public void RaisePropertyChangedWhenEnableDetailedSyncLoggingChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(UpdateAccountDetailsViewModel.EnableDetailedSyncLogging))
            {
                propertyChanged = true;
            }
        };

        sut.EnableDetailedSyncLogging = true;

        propertyChanged.ShouldBeTrue();
        sut.EnableDetailedSyncLogging.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedWhenEnableDebugLoggingChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(UpdateAccountDetailsViewModel.EnableDebugLogging))
            {
                propertyChanged = true;
            }
        };

        sut.EnableDebugLogging = true;

        propertyChanged.ShouldBeTrue();
        sut.EnableDebugLogging.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedWhenStatusMessageChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(UpdateAccountDetailsViewModel.StatusMessage))
            {
                propertyChanged = true;
            }
        };

        sut.StatusMessage = "Test message";

        propertyChanged.ShouldBeTrue();
        sut.StatusMessage.ShouldBe("Test message");
    }

    [Fact]
    public void RaisePropertyChangedWhenIsSuccessChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(UpdateAccountDetailsViewModel.IsSuccess))
            {
                propertyChanged = true;
            }
        };

        sut.IsSuccess = true;

        propertyChanged.ShouldBeTrue();
        sut.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ExecuteCancelCommandAndRaiseRequestCloseEvent()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        var eventRaised = false;

        sut.RequestClose += (_, _) => eventRaised = true;

        _ = sut.CancelCommand.Execute().Subscribe();

        eventRaised.ShouldBeTrue();
    }

    [Fact]
    public void NotLoadEditableFieldsWhenAccountIsSetToNull()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
        {
            LocalSyncPath = @"C:\ExistingPath",
            EnableDetailedSyncLogging = true,
            SelectedAccount = null
        };

        sut.LocalSyncPath.ShouldBe(@"C:\ExistingPath");
        sut.EnableDetailedSyncLogging.ShouldBeTrue();
    }

    [Fact]
    public void DisableUpdateCommandWhenNoAccountIsSelected()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        var canExecute = false;
        _ = sut.UpdateCommand.CanExecute.Subscribe(x => canExecute = x);

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public void DisableUpdateCommandWhenLocalSyncPathIsEmpty()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        var account = new AccountInfo(
            "account-123",
            "Test User",
            @"C:\TestPath",
            true,
            DateTime.UtcNow,
            null,
            false,
            false,
            3,
            50,
            null);

        sut.SelectedAccount = account;
        sut.LocalSyncPath = string.Empty;

        var canExecute = true;
        _ = sut.UpdateCommand.CanExecute.Subscribe(x => canExecute = x);

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public void EnableUpdateCommandWhenAccountIsSelectedAndPathIsNotEmpty()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        var account = new AccountInfo(
            "account-123",
            "Test User",
            @"C:\TestPath",
            true,
            DateTime.UtcNow,
            null,
            false,
            false,
            3,
            50,
            null);

        sut.SelectedAccount = account;
        sut.LocalSyncPath = @"C:\ValidPath";

        var canExecute = false;
        _ = sut.UpdateCommand.CanExecute.Subscribe(x => canExecute = x);

        canExecute.ShouldBeTrue();
    }

    [Fact]
    public void ClampMaxParallelUpDownloadsToMinimumOf1()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockAccountRepo, mockScheduler)
        {
            MaxParallelUpDownloads = 0
        };

        sut.MaxParallelUpDownloads.ShouldBe(1);
    }

    [Fact]
    public void ClampMaxParallelUpDownloadsToMaximumOf10()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockAccountRepo, mockScheduler)
        {
            MaxParallelUpDownloads = 20
        };

        sut.MaxParallelUpDownloads.ShouldBe(10);
    }

    [Fact]
    public void ClampMaxItemsInBatchToMinimumOf1()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockAccountRepo, mockScheduler)
        {
            MaxItemsInBatch = 0
        };

        sut.MaxItemsInBatch.ShouldBe(1);
    }

    [Fact]
    public void ClampMaxItemsInBatchToMaximumOf100()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockAccountRepo, mockScheduler)
        {
            MaxItemsInBatch = 200
        };

        sut.MaxItemsInBatch.ShouldBe(100);
    }

    [Fact]
    public void LoadMaxParallelUpDownloadsWhenAccountSelected()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockAccountRepo, mockScheduler);
        var account = new AccountInfo(
            "acc1",
            "Test User",
            @"C:\Sync",
            true,
            null,
            null,
            false,
            false,
            5,
            75,
            null);

        sut.SelectedAccount = account;

        sut.MaxParallelUpDownloads.ShouldBe(5);
    }

    [Fact]
    public void LoadMaxItemsInBatchWhenAccountSelected()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockAccountRepo, mockScheduler);
        var account = new AccountInfo(
            "acc1",
            "Test User",
            @"C:\Sync",
            true,
            null,
            null,
            false,
            false,
            5,
            75,
            null);

        sut.SelectedAccount = account;

        sut.MaxItemsInBatch.ShouldBe(75);
    }
}
