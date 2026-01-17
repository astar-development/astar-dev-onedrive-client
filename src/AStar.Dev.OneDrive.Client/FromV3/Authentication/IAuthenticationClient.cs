using Microsoft.Identity.Client;

namespace AStar.Dev.OneDrive.Client.FromV3.Authentication;

/// <summary>
/// Wrapper interface for <see cref="IPublicClientApplication"/> to enable testing.
/// </summary>
/// <remarks>
/// MSAL's builder classes (AcquireTokenInteractiveParameterBuilder, etc.) are sealed,
/// making them impossible to mock in tests. This interface provides a testable abstraction
/// over the authentication flows.
/// </remarks>
public interface IAuthenticationClient
{
    /// <summary>
    /// Acquires a token interactively (with UI) for the specified scopes.
    /// </summary>
    /// <param name="scopes">The scopes to request.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The authentication result containing the access token and account information.</returns>
    Task<MsalAuthResult> AcquireTokenInteractiveAsync(IEnumerable<string> scopes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquires a token silently (without UI) for the specified account and scopes.
    /// </summary>
    /// <param name="scopes">The scopes to request.</param>
    /// <param name="account">The account to acquire the token for.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The authentication result containing the access token.</returns>
    Task<MsalAuthResult> AcquireTokenSilentAsync(IEnumerable<string> scopes, IAccount account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all accounts currently in the token cache.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Collection of cached accounts.</returns>
    Task<IEnumerable<IAccount>> GetAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an account from the token cache.
    /// </summary>
    /// <param name="account">The account to remove.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    Task RemoveAsync(IAccount account, CancellationToken cancellationToken = default);
}
