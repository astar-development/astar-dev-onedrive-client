using AStar.Dev.OneDrive.Client.Core.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Auth;

public sealed class MsalAuthService : IAuthService
{
    private readonly MsalConfigurationSettings _msalConfigurationSettings;
    private readonly IPublicClientApplication _pca;

    public MsalAuthService(MsalConfigurationSettings msalConfigurationSettings)
    {
        _msalConfigurationSettings = msalConfigurationSettings;
        _pca = PublicClientApplicationBuilder
            .Create(msalConfigurationSettings.ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, "Common")
            .WithRedirectUri(msalConfigurationSettings.RedirectUri)
            .Build();
    }

    private IAccount? _account;
    private bool _initialized;

    public async Task SignInAsync(CancellationToken ct)
    {
        MsalCacheHelper cacheHelper = await MsalCacheHelper.CreateAsync(
    new StorageCreationPropertiesBuilder(
        $"{_msalConfigurationSettings.CachePrefix}1.bin",
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
    .WithUnprotectedFile()
    .Build());
        cacheHelper.RegisterCache(_pca.UserTokenCache);

        _account = (await _pca.GetAccountsAsync()).FirstOrDefault();

        try
        {
            AuthenticationResult result = await _pca
            .AcquireTokenSilent(_msalConfigurationSettings.Scopes, _account)
            .ExecuteAsync(ct);

            _account = result.Account;
        }
        catch(MsalUiRequiredException)
        {
            AuthenticationResult result = await _pca
            .AcquireTokenInteractive(_msalConfigurationSettings.Scopes)
            .ExecuteAsync(ct);

            _account = result.Account;
        }
    }

    public async Task SignOutAsync(CancellationToken ct)
    {
        IEnumerable<IAccount> accounts = await _pca.GetAccountsAsync();

        foreach(IAccount? account in accounts)
        {
            await _pca.RemoveAsync(account);
        }

        _account = null;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if(_account is null)
            throw new InvalidOperationException("Not signed in");
        AuthenticationResult result = await _pca.AcquireTokenSilent(_msalConfigurationSettings.Scopes, _account).ExecuteAsync(ct);
        return result.AccessToken;
    }

    public async Task<bool> IsUserSignedInAsync(CancellationToken ct)
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
                $"{_msalConfigurationSettings.CachePrefix}1.bin",
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
            .WithUnprotectedFile()
            .Build());

        cacheHelper.RegisterCache(_pca.UserTokenCache);

        _initialized = true;
    }
}
