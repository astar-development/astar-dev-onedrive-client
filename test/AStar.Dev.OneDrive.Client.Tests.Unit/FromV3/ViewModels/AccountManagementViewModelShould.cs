using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.FromV3.Authentication;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;
using AStar.Dev.OneDrive.Client.ViewModels;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.FromV3.ViewModels;

public class AccountManagementViewModelShould
{
    [Fact]
    public async Task InitializeWithEmptyAccountCollectionWhenRepositoryIsEmpty()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None); // Allow async initialization

        viewModel.Accounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task InitializeWithNullSelectedAccount()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        viewModel.SelectedAccount.ShouldBeNull();
    }

    [Fact]
    public async Task LoadAccountsFromRepositoryOnInitialization()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        AccountInfo[] accounts = [CreateAccount("acc1", "User 1"), CreateAccount("acc2", "User 2")];
        _ = mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>(accounts));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        viewModel.Accounts.Count.ShouldBe(2);
        viewModel.Accounts[0].AccountId.ShouldBe("acc1");
        viewModel.Accounts[1].AccountId.ShouldBe("acc2");
    }

    [Fact]
    public async Task RaisePropertyChangedWhenSelectedAccountChanges()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        AccountInfo account = CreateAccount("acc1", "User 1");
        var propertyChanged = false;

        viewModel.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(AccountManagementViewModel.SelectedAccount)) propertyChanged = true;
        };

        viewModel.SelectedAccount = account;

        propertyChanged.ShouldBeTrue();
        viewModel.SelectedAccount.ShouldBe(account);
    }

    [Fact]
    public async Task NotRaisePropertyChangedWhenSettingSameSelectedAccount()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        AccountInfo account = CreateAccount("acc1", "User 1");
        viewModel.SelectedAccount = account;

        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (_, _) => propertyChangedCount++;

        viewModel.SelectedAccount = account;

        propertyChangedCount.ShouldBe(0);
    }

    [Fact]
    public async Task RaisePropertyChangedWhenIsLoadingChanges()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        var propertyChanged = false;

        viewModel.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(AccountManagementViewModel.IsLoading)) propertyChanged = true;
        };

        viewModel.IsLoading = true;

        propertyChanged.ShouldBeTrue();
        viewModel.IsLoading.ShouldBeTrue();
    }

    [Fact]
    public async Task NotRaisePropertyChangedWhenSettingSameIsLoadingValue()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        viewModel.IsLoading = true;

        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (_, _) => propertyChangedCount++;

        viewModel.IsLoading = true;

        propertyChangedCount.ShouldBe(0);
    }

    [Fact]
    public async Task HaveAddAccountCommandAlwaysEnabled()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        var canExecute = viewModel.AddAccountCommand.CanExecute.FirstAsync().Wait();

        canExecute.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAddAccountCommandSuccessfully()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        var authResult = new AuthenticationResult(true, "acc1", "user@example.com", null);
        _ = mockAuth.LoginAsync(Arg.Any<CancellationToken>()).Returns(authResult);

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        _ = await viewModel.AddAccountCommand.Execute();

        _ = await mockAuth.Received(1).LoginAsync(Arg.Any<CancellationToken>());
        await mockRepo.Received(1).AddAsync(Arg.Any<AccountInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HaveRemoveAccountCommandDisabledWhenNoAccountSelected()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        var canExecute = viewModel.RemoveAccountCommand.CanExecute.FirstAsync().Wait();

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public async Task EnableRemoveAccountCommandWhenAccountSelected()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        AccountInfo account = CreateAccount("acc1", "User 1");

        viewModel.SelectedAccount = account;

        var canExecute = viewModel.RemoveAccountCommand.CanExecute.FirstAsync().Wait();
        canExecute.ShouldBeTrue();
    }

    [Fact]
    public async Task DisableRemoveAccountCommandWhenAccountDeselected()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        AccountInfo account = CreateAccount("acc1", "User 1");
        viewModel.SelectedAccount = account;

        viewModel.SelectedAccount = null;

        var canExecute = viewModel.RemoveAccountCommand.CanExecute.FirstAsync().Wait();
        canExecute.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteRemoveAccountCommandWhenAccountSelected()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        AccountInfo account = CreateAccount("acc1", "User 1");
        viewModel.SelectedAccount = account;

        _ = await viewModel.RemoveAccountCommand.Execute();

        await mockRepo.Received(1).DeleteAsync("acc1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HaveLoginCommandDisabledWhenNoAccountSelected()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        var canExecute = viewModel.LoginCommand.CanExecute.FirstAsync().Wait();

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public async Task EnableLoginCommandWhenUnauthenticatedAccountSelected()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        AccountInfo account = CreateAccount("acc1", "User 1", isAuthenticated: false);

        viewModel.SelectedAccount = account;

        var canExecute = viewModel.LoginCommand.CanExecute.FirstAsync().Wait();
        canExecute.ShouldBeTrue();
    }

    [Fact]
    public async Task DisableLoginCommandWhenAuthenticatedAccountSelected()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        AccountInfo account = CreateAccount("acc1", "User 1", isAuthenticated: true);

        viewModel.SelectedAccount = account;

        var canExecute = viewModel.LoginCommand.CanExecute.FirstAsync().Wait();
        canExecute.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteLoginCommandSuccessfully()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        var authResult = new AuthenticationResult(true, "acc1", "User 1", null);
        _ = mockAuth.LoginAsync(Arg.Any<CancellationToken>()).Returns(authResult);

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        AccountInfo account = CreateAccount("acc1", "User 1", isAuthenticated: false);
        viewModel.SelectedAccount = account;

        _ = await viewModel.LoginCommand.Execute();

        _ = await mockAuth.Received(1).LoginAsync(Arg.Any<CancellationToken>());
        await mockRepo.Received(1).UpdateAsync(Arg.Any<AccountInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HaveLogoutCommandDisabledWhenNoAccountSelected()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        var canExecute = viewModel.LogoutCommand.CanExecute.FirstAsync().Wait();

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public async Task EnableLogoutCommandWhenAuthenticatedAccountSelected()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        AccountInfo account = CreateAccount("acc1", "User 1", isAuthenticated: true);

        viewModel.SelectedAccount = account;

        var canExecute = viewModel.LogoutCommand.CanExecute.FirstAsync().Wait();
        canExecute.ShouldBeTrue();
    }

    [Fact]
    public async Task DisableLogoutCommandWhenUnauthenticatedAccountSelected()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        AccountInfo account = CreateAccount("acc1", "User 1", isAuthenticated: false);

        viewModel.SelectedAccount = account;

        var canExecute = viewModel.LogoutCommand.CanExecute.FirstAsync().Wait();
        canExecute.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteLogoutCommandSuccessfully()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        _ = mockAuth.LogoutAsync("acc1", Arg.Any<CancellationToken>()).Returns(true);

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        AccountInfo account = CreateAccount("acc1", "User 1", isAuthenticated: true);
        viewModel.SelectedAccount = account;

        _ = await viewModel.LogoutCommand.Execute();

        _ = await mockAuth.Received(1).LogoutAsync("acc1", Arg.Any<CancellationToken>());
        await mockRepo.Received(1).UpdateAsync(Arg.Any<AccountInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AllowAddingAccountsToCollection()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        AccountInfo account1 = CreateAccount("acc1", "User 1");
        AccountInfo account2 = CreateAccount("acc2", "User 2");

        viewModel.Accounts.Add(account1);
        viewModel.Accounts.Add(account2);

        viewModel.Accounts.Count.ShouldBe(2);
        viewModel.Accounts.ShouldContain(account1);
        viewModel.Accounts.ShouldContain(account2);
    }

    [Fact]
    public async Task AllowRemovingAccountsFromCollection()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        using var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        AccountInfo account = CreateAccount("acc1", "User 1");
        viewModel.Accounts.Add(account);

        _ = viewModel.Accounts.Remove(account);

        viewModel.Accounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task DisposeSuccessfullyWithoutErrors()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));

        var viewModel = new AccountManagementViewModel(mockAuth, mockRepo);
        await Task.Delay(50, CancellationToken.None);

        Should.NotThrow(viewModel.Dispose);
    }

    private static AccountInfo CreateAccount(string id, string displayName, bool isAuthenticated = false)
        => new(id, displayName, $@"C:\Sync\{id}", isAuthenticated, null, null, false, false, 3, 50, null);
}
