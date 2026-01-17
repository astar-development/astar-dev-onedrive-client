using AStar.Dev.OneDrive.Client.FromV3.Models;

namespace AStar.Dev.OneDrive.Client.FromV3.Repositories;

/// <summary>
/// Repository for managing account data.
/// </summary>
public interface IAccountRepository
{
    /// <summary>
    /// Gets all accounts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all accounts.</returns>
    Task<IReadOnlyList<AccountInfo>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an account by its ID.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The account if found, otherwise null.</returns>
    Task<AccountInfo?> GetByIdAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new account.
    /// </summary>
    /// <param name="account">The account to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(AccountInfo account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing account.
    /// </summary>
    /// <param name="account">The account to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(AccountInfo account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an account by its ID.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an account with the specified ID exists.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the account exists, otherwise false.</returns>
    Task<bool> ExistsAsync(string accountId, CancellationToken cancellationToken = default);
}
