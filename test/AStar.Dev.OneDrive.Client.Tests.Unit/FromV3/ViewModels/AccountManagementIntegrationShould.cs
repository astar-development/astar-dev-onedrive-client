using AStar.Dev.OneDrive.Client.FromV3.Authentication;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Repositories;
using AStar.Dev.OneDrive.Client.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.FromV3.ViewModels;

/// <summary>
/// Integration tests for AccountManagementViewModel with real repository and in-memory database.
/// </summary>
public class AccountManagementIntegrationShould : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly AccountRepository _accountRepository;
    private readonly IAuthService _mockAuthService;
    private bool _disposed;

    public AccountManagementIntegrationShould()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.CreateVersion7()}")
            .Options;

        _dbContext = new AppDbContext(options);
        _accountRepository = new AccountRepository(_dbContext);
        _mockAuthService = Substitute.For<IAuthService>();
    }

    [Fact]
    public async Task LoadExistingAccountsFromDatabaseOnInitialization()
    {
        var account1 = new AccountInfo("acc1", "User One", "/path1", true, null, null, false, false, 3, 50, null);
        var account2 = new AccountInfo("acc2", "User Two", "/path2", false, null, null, false, false, 3, 50, null);
        await _accountRepository.AddAsync(account1, CancellationToken.None);
        await _accountRepository.AddAsync(account2, CancellationToken.None);

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(100, CancellationToken.None);

        viewModel.Accounts.Count.ShouldBe(2);
        viewModel.Accounts.ShouldContain(a => a.AccountId == "acc1");
        viewModel.Accounts.ShouldContain(a => a.AccountId == "acc2");
    }

    [Fact]
    public async Task PersistNewAccountToDatabaseWhenAdded()
    {
        var authResult = new AuthenticationResult(true, "new-acc", "New User", null);
        _ = _mockAuthService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(authResult));

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50, CancellationToken.None);

        _ = viewModel.AddAccountCommand.Execute().Subscribe();
        await Task.Delay(100, CancellationToken.None);

        IReadOnlyList<AccountInfo> accounts = await _accountRepository.GetAllAsync();
        accounts.Count.ShouldBe(1);
        accounts[0].AccountId.ShouldBe("new-acc");
        accounts[0].DisplayName.ShouldBe("New User");
        accounts[0].IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveAccountFromDatabaseWhenDeleted()
    {
        var account = new AccountInfo("acc-to-delete", "User", "/path", true, null, null, false, false, 3, 50, null);
        await _accountRepository.AddAsync(account, CancellationToken.None);

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50, CancellationToken.None);

        viewModel.SelectedAccount = viewModel.Accounts.First();
        _ = viewModel.RemoveAccountCommand.Execute().Subscribe();
        await Task.Delay(100, CancellationToken.None);

        IReadOnlyList<AccountInfo> accounts = await _accountRepository.GetAllAsync();
        accounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateAuthenticationStateInDatabaseWhenLoggingIn()
    {
        var account = new AccountInfo("acc-login", "User", "/path", false, null, null, false, false, 3, 50, null);
        await _accountRepository.AddAsync(account, CancellationToken.None);

        var authResult = new AuthenticationResult(true, null, null, null);
        _ = _mockAuthService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(authResult));

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50, CancellationToken.None);

        viewModel.SelectedAccount = viewModel.Accounts.First();
        _ = viewModel.LoginCommand.Execute().Subscribe();
        await Task.Delay(100, CancellationToken.None);
        AccountInfo? updatedAccount = await _accountRepository.GetByIdAsync("acc-login", CancellationToken.None);
        _ = updatedAccount.ShouldNotBeNull();
        updatedAccount.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateAuthenticationStateInDatabaseWhenLoggingOut()
    {
        var account = new AccountInfo("acc-logout", "User", "/path", true, null, null, false, false, 3, 50, null);
        await _accountRepository.AddAsync(account, CancellationToken.None);

        _ = _mockAuthService.LogoutAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50, CancellationToken.None);

        viewModel.SelectedAccount = viewModel.Accounts.First();
        _ = viewModel.LogoutCommand.Execute().Subscribe();
        await Task.Delay(100, CancellationToken.None);
        AccountInfo? updatedAccount = await _accountRepository.GetByIdAsync("acc-logout", CancellationToken.None);
        _ = updatedAccount.ShouldNotBeNull();
        updatedAccount.IsAuthenticated.ShouldBeFalse();
    }

    [Fact]
    public async Task MaintainConsistencyBetweenViewModelAndDatabaseAfterMultipleOperations()
    {
        var authResult = new AuthenticationResult(true, "multi-acc", "Multi User", null);
        _ = _mockAuthService.LoginAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(authResult));
        _ = _mockAuthService.LogoutAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50, CancellationToken.None);

        _ = viewModel.AddAccountCommand.Execute().Subscribe();
        await Task.Delay(100, CancellationToken.None);

        viewModel.SelectedAccount = viewModel.Accounts.First();
        _ = viewModel.LogoutCommand.Execute().Subscribe();
        await Task.Delay(100, CancellationToken.None);
        _ = viewModel.LoginCommand.Execute().Subscribe();
        await Task.Delay(100, CancellationToken.None);

        IReadOnlyList<AccountInfo> dbAccounts = await _accountRepository.GetAllAsync();
        viewModel.Accounts.Count.ShouldBe(1);
        dbAccounts.Count.ShouldBe(1);
        dbAccounts[0].AccountId.ShouldBe("multi-acc");
        dbAccounts[0].IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleEmptyDatabaseGracefully()
    {
        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50, CancellationToken.None);

        viewModel.Accounts.ShouldBeEmpty();
        viewModel.SelectedAccount.ShouldBeNull();
        viewModel.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public async Task PreserveAccountDataIntegrityAfterReload()
    {
        var account = new AccountInfo(
            "preserve-acc",
            "Preserve User",
            "/custom/path",
            true,
            DateTime.UtcNow,
            "delta-token-123",
            false,
            false,
            3,
            50,
            null);
        await _accountRepository.AddAsync(account, CancellationToken.None);

        using var viewModel = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        await Task.Delay(50, CancellationToken.None);

        AccountInfo loadedAccount = viewModel.Accounts.First();
        loadedAccount.AccountId.ShouldBe("preserve-acc");
        loadedAccount.DisplayName.ShouldBe("Preserve User");
        loadedAccount.LocalSyncPath.ShouldBe("/custom/path");
        loadedAccount.DeltaToken.ShouldBe("delta-token-123");
        _ = loadedAccount.LastSyncUtc.ShouldNotBeNull();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if(_disposed)
        {
            return;
        }

        if(disposing)
        {
            _dbContext?.Dispose();
        }

        _disposed = true;
    }
}
