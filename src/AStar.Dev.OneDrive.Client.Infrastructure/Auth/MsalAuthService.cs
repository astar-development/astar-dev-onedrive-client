using AStar.Dev.OneDrive.Client.Core.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.Identity.Client;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Auth;

public sealed class MsalAuthService(MsalConfigurationSettings msalConfigurationSettings) : IAuthService
{
    private readonly IPublicClientApplication _pca = PublicClientApplicationBuilder.Create(msalConfigurationSettings.ClientId)
            .WithRedirectUri(msalConfigurationSettings.RedirectUri)
            .Build();
    private IAccount? _account;
    private readonly string[] _scopes = msalConfigurationSettings.Scopes;
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
