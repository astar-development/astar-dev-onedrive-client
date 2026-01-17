namespace AStar.Dev.OneDrive.Client.FromV3.Authentication;

/// <summary>
/// Service for managing Microsoft authentication via MSAL.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Initiates interactive login flow for a new account.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication result containing account information.</returns>
    Task<AuthenticationResult> LoginAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out an account by removing it from the token cache.
    /// </summary>
    /// <param name="accountId">The account identifier to log out.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if logout was successful, false otherwise.</returns>
    Task<bool> LogoutAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all authenticated accounts from the token cache.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of account identifiers and display names.</returns>
    Task<IReadOnlyList<(string AccountId, string DisplayName)>> GetAuthenticatedAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquires an access token for Microsoft Graph API calls.
    /// </summary>
    /// <param name="accountId">The account identifier to get a token for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Access token if successful, null otherwise.</returns>
    Task<string?> GetAccessTokenAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an account is currently authenticated.
    /// </summary>
    /// <param name="accountId">The account identifier to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the account is authenticated, false otherwise.</returns>
    Task<bool> IsAuthenticatedAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquires a token silently (without user interaction) if possible.
    /// </summary>
    /// <param name="accountId">The account identifier to get a token for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Access token if successful, null if user interaction is required.</returns>
    Task<string?> AcquireTokenSilentAsync(string accountId, CancellationToken cancellationToken = default);
}
