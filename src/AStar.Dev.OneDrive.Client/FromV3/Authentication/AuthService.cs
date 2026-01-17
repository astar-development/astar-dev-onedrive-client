using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace AStar.Dev.OneDrive.Client.FromV3.Authentication;

/// <summary>
/// Service for managing Microsoft authentication via MSAL.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IAuthenticationClient _authClient;
    private readonly AuthConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="authClient">The authentication client wrapper.</param>
    /// <param name="configuration">Authentication configuration.</param>
    public AuthService(IAuthenticationClient authClient, AuthConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(authClient);
        ArgumentNullException.ThrowIfNull(configuration);
        _authClient = authClient;
        _configuration = configuration;
    }

    /// <summary>
    /// Creates a new AuthService with default MSAL configuration.
    /// </summary>
    /// <param name="configuration">Authentication configuration.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Configured AuthService instance.</returns>
    public static async Task<AuthService> CreateAsync(AuthConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        IPublicClientApplication app = PublicClientApplicationBuilder
            .Create(configuration.ClientId)
            .WithAuthority(configuration.Authority)
            .WithRedirectUri(configuration.RedirectUri)
            .Build();

        // Setup token cache persistence
        var cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AStar.Dev.OneDrive.Client");

        var storagePropertiesBuilder = new StorageCreationPropertiesBuilder(
            "astar_onedrive_cache.dat",
            cacheDirectory);

        // Use plaintext storage on Linux due to keyring/libsecret compatibility issues
        // Windows and macOS use platform-specific secure storage (DPAPI and Keychain)
        if(OperatingSystem.IsLinux())
        {
            _ = storagePropertiesBuilder.WithUnprotectedFile();
        }

        StorageCreationProperties storageProperties = storagePropertiesBuilder.Build();
        MsalCacheHelper cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        cacheHelper.RegisterCache(app.UserTokenCache);

        return new AuthService(new AuthenticationClient(app), configuration);
    }

    /// <inheritdoc/>
    public async Task<AuthenticationResult> LoginAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            MsalAuthResult result = await _authClient
                .AcquireTokenInteractiveAsync(_configuration.Scopes, cts.Token);

            return new AuthenticationResult(
                Success: true,
                AccountId: result.Account.HomeAccountId.Identifier,
                DisplayName: result.Account.Username,
                ErrorMessage: null
            );
        }
        catch(MsalException ex)
        {
            return new AuthenticationResult(
                Success: false,
                AccountId: null,
                DisplayName: null,
                ErrorMessage: ex.Message
            );
        }
        catch(OperationCanceledException)
        {
            var message = cancellationToken.IsCancellationRequested
                ? "Login was cancelled."
                : "Login timed out after 30 seconds.";

            return new AuthenticationResult(
                Success: false,
                AccountId: null,
                DisplayName: null,
                ErrorMessage: message
            );
        }
    }

    /// <inheritdoc/>
    public async Task<bool> LogoutAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        try
        {
            IEnumerable<IAccount> accounts = await _authClient.GetAccountsAsync(cancellationToken);
            IAccount? account = accounts.FirstOrDefault(a => a.HomeAccountId.Identifier == accountId);

            if(account is not null)
            {
                await _authClient.RemoveAsync(account, cancellationToken);
                return true;
            }

            return false;
        }
        catch(MsalException)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<(string AccountId, string DisplayName)>> GetAuthenticatedAccountsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IEnumerable<IAccount> accounts = await _authClient.GetAccountsAsync(cancellationToken);
            return accounts
                .Select(a => (a.HomeAccountId.Identifier, a.Username))
                .ToList();
        }
        catch(MsalException)
        {
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetAccessTokenAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        try
        {
            IEnumerable<IAccount> accounts = await _authClient.GetAccountsAsync(cancellationToken);
            IAccount? account = accounts.FirstOrDefault(a => a.HomeAccountId.Identifier == accountId);

            if(account is null)
            {
                return null;
            }

            MsalAuthResult result = await _authClient
                .AcquireTokenSilentAsync(_configuration.Scopes, account, cancellationToken);

            return result.AccessToken;
        }
        catch(MsalUiRequiredException)
        {
            return null;
        }
        catch(MsalException)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsAuthenticatedAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        try
        {
            IEnumerable<IAccount> accounts = await _authClient.GetAccountsAsync(cancellationToken);
            return accounts.Any(a => a.HomeAccountId.Identifier == accountId);
        }
        catch(MsalException)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> AcquireTokenSilentAsync(string accountId, CancellationToken cancellationToken = default) => await GetAccessTokenAsync(accountId, cancellationToken);
}
