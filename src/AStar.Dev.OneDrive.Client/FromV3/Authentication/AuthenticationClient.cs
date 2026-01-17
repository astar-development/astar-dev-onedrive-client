using Microsoft.Identity.Client;

namespace AStar.Dev.OneDrive.Client.FromV3.Authentication;

/// <summary>
/// Wrapper implementation for <see cref="IPublicClientApplication"/> that delegates to MSAL.
/// </summary>
/// <remarks>
/// This wrapper exists because MSAL's builder classes are sealed and cannot be mocked in tests.
/// By wrapping the interaction with MSAL behind this interface, we can inject mocks for testing.
/// </remarks>
public sealed class AuthenticationClient : IAuthenticationClient
{
    private readonly IPublicClientApplication _publicClientApp;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationClient"/> class.
    /// </summary>
    /// <param name="publicClientApp">The MSAL public client application instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when publicClientApp is null.</exception>
    public AuthenticationClient(IPublicClientApplication publicClientApp)
    {
        ArgumentNullException.ThrowIfNull(publicClientApp);
        _publicClientApp = publicClientApp;
    }

    /// <inheritdoc/>
    public async Task<MsalAuthResult> AcquireTokenInteractiveAsync(IEnumerable<string> scopes, CancellationToken cancellationToken = default)
    {
        Microsoft.Identity.Client.AuthenticationResult result = await _publicClientApp
            .AcquireTokenInteractive(scopes)
            .ExecuteAsync(cancellationToken);

        return MsalAuthResult.FromMsal(result);
    }

    /// <inheritdoc/>
    public async Task<MsalAuthResult> AcquireTokenSilentAsync(IEnumerable<string> scopes, IAccount account, CancellationToken cancellationToken = default)
    {
        Microsoft.Identity.Client.AuthenticationResult result = await _publicClientApp
            .AcquireTokenSilent(scopes, account)
            .ExecuteAsync(cancellationToken);

        return MsalAuthResult.FromMsal(result);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<IAccount>> GetAccountsAsync(CancellationToken cancellationToken = default) => await _publicClientApp.GetAccountsAsync();

    /// <inheritdoc/>
    public async Task RemoveAsync(IAccount account, CancellationToken cancellationToken = default) => await _publicClientApp.RemoveAsync(account);
}
