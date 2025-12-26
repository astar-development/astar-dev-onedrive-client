using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.Identity.Client;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Auth;

public sealed class MsalAuthService(string clientId) : IAuthService
{
#pragma warning disable S1075 // URIs should not be hardcoded - Required by MSAL for local OAuth redirect
    private const string RedirectUri = "http://localhost";
#pragma warning restore S1075
    private readonly IPublicClientApplication _pca = PublicClientApplicationBuilder.Create(clientId)
            .WithRedirectUri(RedirectUri)
            .Build();
    private IAccount? _account;
    private readonly string[] _scopes = ["Files.ReadWrite.All", "offline_access", "User.Read"];

    public bool IsSignedIn => _account is not null;

    public async Task SignInAsync(CancellationToken ct)
    {
        AuthenticationResult result = await _pca.AcquireTokenInteractive(_scopes).ExecuteAsync(ct);
        _account = result.Account;
    }

    public async Task SignOutAsync(CancellationToken ct)
    {
        if(_account is null)
            return;
        await _pca.RemoveAsync(_account);
        _account = null;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if(_account is null)
            throw new InvalidOperationException("Not signed in");
        AuthenticationResult result = await _pca.AcquireTokenSilent(_scopes, _account).ExecuteAsync(ct);
        return result.AccessToken;
    }
}
