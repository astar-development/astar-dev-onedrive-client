using AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;
using AStar.Dev.OneDrive.Client.ViewModels;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.FromV3.ViewModels;

public class ViewSyncHistoryViewModelShould
{
    [Fact]
    public void ThrowArgumentNullExceptionWhenAccountRepositoryIsNull()
    {
        IFileOperationLogRepository mockFileOpLogRepo = Substitute.For<IFileOperationLogRepository>();

        ArgumentNullException exception = Should.Throw<ArgumentNullException>(() =>
            new ViewSyncHistoryViewModel(null!, mockFileOpLogRepo));

        exception.ParamName.ShouldBe("accountRepository");
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenFileOperationLogRepositoryIsNull()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();

        ArgumentNullException exception = Should.Throw<ArgumentNullException>(() =>
            new ViewSyncHistoryViewModel(mockAccountRepo, null!));

        exception.ParamName.ShouldBe("fileOperationLogRepository");
    }

    [Fact]
    public void InitializeWithEmptyAccountsCollection()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IFileOperationLogRepository mockFileOpLogRepo = Substitute.For<IFileOperationLogRepository>();

        var sut = new ViewSyncHistoryViewModel(mockAccountRepo, mockFileOpLogRepo);

        sut.Accounts.ShouldBeEmpty();
    }

    [Fact]
    public void InitializeWithEmptySyncHistoryCollection()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IFileOperationLogRepository mockFileOpLogRepo = Substitute.For<IFileOperationLogRepository>();

        var sut = new ViewSyncHistoryViewModel(mockAccountRepo, mockFileOpLogRepo);

        sut.SyncHistory.ShouldBeEmpty();
    }

    [Fact]
    public void InitializeWithNullSelectedAccount()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IFileOperationLogRepository mockFileOpLogRepo = Substitute.For<IFileOperationLogRepository>();

        var sut = new ViewSyncHistoryViewModel(mockAccountRepo, mockFileOpLogRepo);

        sut.SelectedAccount.ShouldBeNull();
    }

    [Fact]
    public void InitializeWithCurrentPageOne()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IFileOperationLogRepository mockFileOpLogRepo = Substitute.For<IFileOperationLogRepository>();

        var sut = new ViewSyncHistoryViewModel(mockAccountRepo, mockFileOpLogRepo);

        sut.CurrentPage.ShouldBe(1);
    }

    [Fact]
    public void InitializeWithHasMoreRecordsTrue()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IFileOperationLogRepository mockFileOpLogRepo = Substitute.For<IFileOperationLogRepository>();

        var sut = new ViewSyncHistoryViewModel(mockAccountRepo, mockFileOpLogRepo);

        sut.HasMoreRecords.ShouldBeTrue();
    }

    [Fact]
    public void InitializeWithIsLoadingFalse()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IFileOperationLogRepository mockFileOpLogRepo = Substitute.For<IFileOperationLogRepository>();

        var sut = new ViewSyncHistoryViewModel(mockAccountRepo, mockFileOpLogRepo);

        sut.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void InitializeLoadNextPageCommand()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IFileOperationLogRepository mockFileOpLogRepo = Substitute.For<IFileOperationLogRepository>();

        var sut = new ViewSyncHistoryViewModel(mockAccountRepo, mockFileOpLogRepo);

        _ = sut.LoadNextPageCommand.ShouldNotBeNull();
    }

    [Fact]
    public void InitializeLoadPreviousPageCommand()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IFileOperationLogRepository mockFileOpLogRepo = Substitute.For<IFileOperationLogRepository>();

        var sut = new ViewSyncHistoryViewModel(mockAccountRepo, mockFileOpLogRepo);

        _ = sut.LoadPreviousPageCommand.ShouldNotBeNull();
    }

    [Fact]
    public void ReturnCanGoToPreviousPageFalseWhenOnFirstPage()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IFileOperationLogRepository mockFileOpLogRepo = Substitute.For<IFileOperationLogRepository>();

        var sut = new ViewSyncHistoryViewModel(mockAccountRepo, mockFileOpLogRepo);

        sut.CanGoToPreviousPage.ShouldBeFalse();
    }

    [Fact]
    public void ReturnCanGoToNextPageTrueWhenHasMoreRecords()
    {
        IAccountRepository mockAccountRepo = Substitute.For<IAccountRepository>();
        IFileOperationLogRepository mockFileOpLogRepo = Substitute.For<IFileOperationLogRepository>();

        var sut = new ViewSyncHistoryViewModel(mockAccountRepo, mockFileOpLogRepo);

        sut.CanGoToNextPage.ShouldBeTrue();
    }
}
