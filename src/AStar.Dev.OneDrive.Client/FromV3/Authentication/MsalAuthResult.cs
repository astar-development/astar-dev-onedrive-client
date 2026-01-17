using Microsoft.Identity.Client;

namespace AStar.Dev.OneDrive.Client.FromV3.Authentication;

/// <summary>
/// Simplified authentication result wrapper to avoid dependencies on MSAL's sealed types in tests.
/// </summary>
public sealed class MsalAuthResult
{
    /// <summary>
    /// Gets the authenticated account.
    /// </summary>
    public IAccount Account { get; init; }

    /// <summary>
    /// Gets the access token.
    /// </summary>
    public string AccessToken { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MsalAuthResult"/> class.
    /// </summary>
    /// <param name="account">The authenticated account.</param>
    /// <param name="accessToken">The access token.</param>
    public MsalAuthResult(IAccount account, string accessToken)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(accessToken);

        Account = account;
        AccessToken = accessToken;
    }

    /// <summary>
    /// Creates a wrapper from MSAL's AuthenticationResult.
    /// </summary>
    /// <param name="msalResult">The MSAL authentication result.</param>
    /// <returns>Wrapped result.</returns>
    public static MsalAuthResult FromMsal(Microsoft.Identity.Client.AuthenticationResult msalResult)
    {
        ArgumentNullException.ThrowIfNull(msalResult);
        return new MsalAuthResult(msalResult.Account, msalResult.AccessToken);
    }
}
