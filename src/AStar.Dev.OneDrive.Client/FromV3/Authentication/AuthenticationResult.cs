namespace AStar.Dev.OneDrive.Client.FromV3.Authentication;

/// <summary>
/// Result of an authentication operation.
/// </summary>
/// <param name="Success">Indicates whether the operation was successful.</param>
/// <param name="AccountId">The authenticated account identifier.</param>
/// <param name="DisplayName">The display name of the authenticated user.</param>
/// <param name="ErrorMessage">Error message if the operation failed.</param>
public sealed record AuthenticationResult(
    bool Success,
    string? AccountId,
    string? DisplayName,
    string? ErrorMessage
);
