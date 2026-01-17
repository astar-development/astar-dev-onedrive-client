using AStar.Dev.OneDrive.Client.FromV3.Authentication;
using Microsoft.Identity.Client;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.FromV3.Authentication;

public class AuthServiceShould
{
    private static AuthConfiguration CreateTestConfiguration()
        => new()
        {
            ClientId = "test-client-id",
            RedirectUri = "http://localhost",
            Authority = "https://login.microsoftonline.com/common",
            Scopes = ["test.scope"]
        };

    [Fact]
    public void ThrowArgumentNullExceptionWhenAuthClientIsNull()
    {
        ArgumentNullException exception = Should.Throw<ArgumentNullException>(
            () => new AuthService(null!, CreateTestConfiguration())
        );

        exception.ParamName.ShouldBe("authClient");
    }

    [Fact]
    public async Task ReturnSuccessResultWhenLoginSucceeds()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        IAccount mockAccount = CreateMockAccount("acc1", "user@example.com");
        var mockAuthResult = new MsalAuthResult(mockAccount, "access_token");

        _ = mockClient.AcquireTokenInteractiveAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(mockAuthResult);

        var service = new AuthService(mockClient, CreateTestConfiguration());

        Client.FromV3.Authentication.AuthenticationResult result = await service.LoginAsync();

        result.Success.ShouldBeTrue();
        result.AccountId.ShouldBe("acc1");
        result.DisplayName.ShouldBe("user@example.com");
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ReturnFailureResultWhenLoginThrowsMsalException()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        _ = mockClient.AcquireTokenInteractiveAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<MsalAuthResult>(new MsalException("login_failed")));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        Client.FromV3.Authentication.AuthenticationResult result = await service.LoginAsync();

        result.Success.ShouldBeFalse();
        result.AccountId.ShouldBeNull();
        result.DisplayName.ShouldBeNull();
        _ = result.ErrorMessage.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnFailureResultWhenLoginIsCancelled()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = mockClient.AcquireTokenInteractiveAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<MsalAuthResult>(new OperationCanceledException()));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        Client.FromV3.Authentication.AuthenticationResult result = await service.LoginAsync(cts.Token);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Login was cancelled.");
    }

    [Fact]
    public async Task ReturnTrueWhenLogoutSucceeds()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        IAccount mockAccount = CreateMockAccount("acc1", "user@example.com");

        _ = mockClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>([mockAccount]));
        _ = mockClient.RemoveAsync(mockAccount, CancellationToken.None).Returns(Task.CompletedTask);

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.LogoutAsync("acc1", CancellationToken.None);

        result.ShouldBeTrue();
        await mockClient.Received(1).RemoveAsync(mockAccount, CancellationToken.None);
    }

    [Fact]
    public async Task ReturnFalseWhenLogoutAccountNotFound()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();

        _ = mockClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>([]));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.LogoutAsync("nonexistent", CancellationToken.None);
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ReturnEmptyListWhenNoAccountsAuthenticated()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        _ = mockClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>([]));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        IReadOnlyList<(string AccountId, string DisplayName)> result = await service.GetAuthenticatedAccountsAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReturnAuthenticatedAccountsCorrectly()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        IAccount account1 = CreateMockAccount("acc1", "user1@example.com");
        IAccount account2 = CreateMockAccount("acc2", "user2@example.com");

        _ = mockClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>([account1, account2]));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        IReadOnlyList<(string AccountId, string DisplayName)> result = await service.GetAuthenticatedAccountsAsync();
        result.Count.ShouldBe(2);
        result[0].AccountId.ShouldBe("acc1");
        result[0].DisplayName.ShouldBe("user1@example.com");
        result[1].AccountId.ShouldBe("acc2");
        result[1].DisplayName.ShouldBe("user2@example.com");
    }

    [Fact]
    public async Task ReturnAccessTokenWhenGetAccessTokenSucceeds()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        IAccount mockAccount = CreateMockAccount("acc1", "user@example.com");
        var mockAuthResult = new MsalAuthResult(mockAccount, "token123");

        _ = mockClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>([mockAccount]));
        _ = mockClient.AcquireTokenSilentAsync(Arg.Any<IEnumerable<string>>(), mockAccount, Arg.Any<CancellationToken>())
            .Returns(mockAuthResult);

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.GetAccessTokenAsync("acc1", CancellationToken.None);

        result.ShouldBe("token123");
    }

    [Fact]
    public async Task ReturnNullWhenGetAccessTokenAccountNotFound()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        _ = mockClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>([]));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.GetAccessTokenAsync("nonexistent", CancellationToken.None);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ReturnNullWhenGetAccessTokenThrowsMsalUiRequiredException()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        IAccount mockAccount = CreateMockAccount("acc1", "user@example.com");

        _ = mockClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>([mockAccount]));
        _ = mockClient.AcquireTokenSilentAsync(Arg.Any<IEnumerable<string>>(), mockAccount, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<MsalAuthResult>(
                new MsalUiRequiredException("error_code", "User interaction required")));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.GetAccessTokenAsync("acc1", CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ReturnFalseWhenAccountNotAuthenticated()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        _ = mockClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>([]));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.IsAuthenticatedAsync("acc1", CancellationToken.None);
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ReturnTrueWhenAccountIsAuthenticated()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        IAccount mockAccount = CreateMockAccount("acc1", "user@example.com");

        _ = mockClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>([mockAccount]));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.IsAuthenticatedAsync("acc1", CancellationToken.None);
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task AcquireTokenSilentlyCallsGetAccessToken()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        IAccount mockAccount = CreateMockAccount("acc1", "user@example.com");
        var mockAuthResult = new MsalAuthResult(mockAccount, "token456");

        _ = mockClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>([mockAccount]));
        _ = mockClient.AcquireTokenSilentAsync(Arg.Any<IEnumerable<string>>(), mockAccount, Arg.Any<CancellationToken>())
            .Returns(mockAuthResult);

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.AcquireTokenSilentAsync("acc1", CancellationToken.None);

        result.ShouldBe("token456");
    }

    private static IAccount CreateMockAccount(string accountId, string username)
    {
        IAccount account = Substitute.For<IAccount>();
        _ = account.HomeAccountId.Returns(new AccountId(accountId, accountId, accountId));
        _ = account.Username.Returns(username);
        return account;
    }
}
