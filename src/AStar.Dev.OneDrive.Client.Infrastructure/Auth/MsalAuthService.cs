using AStar.Dev.OneDrive.Client.Core.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Auth;

public sealed class MsalAuthService(MsalConfigurationSettings msalConfigurationSettings) : IAuthService
{
    private readonly IPublicClientApplication _pca = PublicClientApplicationBuilder
            .Create(msalConfigurationSettings.ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, "Common")
            .WithRedirectUri(msalConfigurationSettings.RedirectUri)
            .Build();
    private IAccount? _account;
    private bool _initialized;

    public async Task SignInAsync(CancellationToken cancellationToken)
    {
        MsalCacheHelper cacheHelper = await MsalCacheHelper.CreateAsync(
    new StorageCreationPropertiesBuilder(
        $"{msalConfigurationSettings.CachePrefix}1.bin",
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
    .WithUnprotectedFile()
    .Build());
        cacheHelper.RegisterCache(_pca.UserTokenCache);

        _account = (await _pca.GetAccountsAsync()).FirstOrDefault();

        try
        {
            AuthenticationResult result = await _pca
            .AcquireTokenSilent(msalConfigurationSettings.Scopes, _account)
            .ExecuteAsync(cancellationToken);

            _account = result.Account;
        }
        catch(MsalUiRequiredException)
        {
            AuthenticationResult result = await _pca
            .AcquireTokenInteractive(msalConfigurationSettings.Scopes)
            .ExecuteAsync(cancellationToken);

            _account = result.Account;
        }
    }

    public async Task SignOutAsync(string accountId, CancellationToken cancellationToken)
    {
        IEnumerable<IAccount> accounts = await _pca.GetAccountsAsync();

        foreach(IAccount? account in accounts) await _pca.RemoveAsync(account);

        _account = null;
    }

    public async Task<string> GetAccessTokenAsync(string accountId, CancellationToken cancellationToken)
    {
        if(_account is null)
            throw new InvalidOperationException("Not signed in");
        AuthenticationResult result = await _pca.AcquireTokenSilent(msalConfigurationSettings.Scopes, _account).ExecuteAsync(cancellationToken);
        return result.AccessToken;
    }

    public async Task<bool> IsUserSignedInAsync(string accountId, CancellationToken cancellationToken)
    {
        await InitializeAsync(); // 🔑 CRITICAL

        _account = (await _pca.GetAccountsAsync()).FirstOrDefault();
        return _account != null;
    }

    private async Task InitializeAsync()
    {
        if(_initialized)
            return;

        MsalCacheHelper cacheHelper = await MsalCacheHelper.CreateAsync(
            new StorageCreationPropertiesBuilder(
                $"{msalConfigurationSettings.CachePrefix}1.bin",
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
            .WithUnprotectedFile()
            .Build());

        cacheHelper.RegisterCache(_pca.UserTokenCache);

        _initialized = true;
    }
}
