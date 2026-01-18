using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;
using AStar.Dev.OneDrive.Client.ViewModels;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.FromV3.ViewModels;

public class DebugLogViewModelShould
{
    [Fact]
    public void ThrowWhenAccountRepositoryIsNull()
    {
        IDebugLogRepository mockDebugLogRepo = Substitute.For<IDebugLogRepository>();

        ArgumentNullException exception = Should.Throw<ArgumentNullException>(() =>
            new DebugLogViewModel(null!, mockDebugLogRepo));

        exception.ParamName.ShouldBe("accountRepository");
    }

    [Fact]
    public void ThrowWhenDebugLogRepositoryIsNull()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();

        ArgumentNullException exception = Should.Throw<ArgumentNullException>(() =>
            new DebugLogViewModel(mockAccountRepo, null!));

        exception.ParamName.ShouldBe("debugLogRepository");
    }

    [Fact]
    public void InitializeWithEmptyCollections()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IDebugLogRepository mockDebugLogRepo = Substitute.For<IDebugLogRepository>();

        var sut = new DebugLogViewModel(mockAccountRepo, mockDebugLogRepo);

        sut.Accounts.ShouldBeEmpty();
        sut.DebugLogs.ShouldBeEmpty();
    }

    [Fact]
    public void InitializeWithCurrentPageAsOne()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IDebugLogRepository mockDebugLogRepo = Substitute.For<IDebugLogRepository>();

        var sut = new DebugLogViewModel(mockAccountRepo, mockDebugLogRepo);

        sut.CurrentPage.ShouldBe(1);
    }

    [Fact]
    public void InitializeWithHasMoreRecordsAsTrue()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IDebugLogRepository mockDebugLogRepo = Substitute.For<IDebugLogRepository>();

        var sut = new DebugLogViewModel(mockAccountRepo, mockDebugLogRepo);

        sut.HasMoreRecords.ShouldBeTrue();
    }

    [Fact]
    public void InitializeWithIsLoadingAsFalse()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IDebugLogRepository mockDebugLogRepo = Substitute.For<IDebugLogRepository>();

        var sut = new DebugLogViewModel(mockAccountRepo, mockDebugLogRepo);

        sut.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void InitializeWithSelectedAccountAsNull()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IDebugLogRepository mockDebugLogRepo = Substitute.For<IDebugLogRepository>();

        var sut = new DebugLogViewModel(mockAccountRepo, mockDebugLogRepo);

        sut.SelectedAccount.ShouldBeNull();
    }

    [Fact]
    public void InitializeCommandsAsNotNull()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IDebugLogRepository mockDebugLogRepo = Substitute.For<IDebugLogRepository>();

        var sut = new DebugLogViewModel(mockAccountRepo, mockDebugLogRepo);

        _ = sut.LoadNextPageCommand.ShouldNotBeNull();
        _ = sut.LoadPreviousPageCommand.ShouldNotBeNull();
        _ = sut.ClearLogsCommand.ShouldNotBeNull();
    }

    [Fact]
    public void RaisePropertyChangedWhenSelectedAccountChanges()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IDebugLogRepository mockDebugLogRepo = Substitute.For<IDebugLogRepository>();
        var sut = new DebugLogViewModel(mockAccountRepo, mockDebugLogRepo);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(DebugLogViewModel.SelectedAccount)) propertyChanged = true;
        };

        var account = new AccountInfo("acc1", "Test", @"C:\Path", true, null, null, false, false, 3, 50, null);
        sut.SelectedAccount = account;

        propertyChanged.ShouldBeTrue();
        sut.SelectedAccount.ShouldBe(account);
    }

    [Fact]
    public async Task RaisePropertyChangedWhenCurrentPageChanges()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IDebugLogRepository mockDebugLogRepo = Substitute.For<IDebugLogRepository>();
        var sut = new DebugLogViewModel(mockAccountRepo, mockDebugLogRepo);
        var propertyChanged = false;

        // Set up account and load initial data - return 51 records to indicate HasMoreRecords
        var account = new AccountInfo("acc1", "Test", @"C:\Path", true, null, null, false, false, 3, 50, null);
        _ = mockAccountRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns([account]);

        var logs = new List<DebugLogEntry>();
        for(var i = 0; i < 51; i++) logs.Add(new DebugLogEntry(i, "acc1", DateTime.UtcNow, "Info", "Test", $"Message {i}", null));

        _ = mockDebugLogRepo.GetByAccountIdAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(logs);

        sut.SelectedAccount = account;

        // Now subscribe to property changes
        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(DebugLogViewModel.CurrentPage)) propertyChanged = true;
        };

        // Trigger page change by going to next page - need to await the observable
        _ = await sut.LoadNextPageCommand.Execute().FirstAsync();

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void ReturnCanGoToNextPageAsTrueWhenHasMoreRecords()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IDebugLogRepository mockDebugLogRepo = Substitute.For<IDebugLogRepository>();
        var sut = new DebugLogViewModel(mockAccountRepo, mockDebugLogRepo);

        sut.CanGoToNextPage.ShouldBeTrue();
    }

    [Fact]
    public void ReturnCanGoToPreviousPageAsFalseWhenOnFirstPage()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IDebugLogRepository mockDebugLogRepo = Substitute.For<IDebugLogRepository>();
        var sut = new DebugLogViewModel(mockAccountRepo, mockDebugLogRepo);

        sut.CanGoToPreviousPage.ShouldBeFalse();
    }
}
