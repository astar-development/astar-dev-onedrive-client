namespace AStar.Dev.OneDrive.Client.Core.Interfaces;

public interface IAuthService
{
    Task SignInAsync(CancellationToken cancellationToken);
    Task SignOutAsync(string accountId, CancellationToken cancellationToken);
    Task<string> GetAccessTokenAsync(string accountId, CancellationToken cancellationToken);
    Task<bool> IsUserSignedInAsync(string accountId, CancellationToken cancellationToken);
}
