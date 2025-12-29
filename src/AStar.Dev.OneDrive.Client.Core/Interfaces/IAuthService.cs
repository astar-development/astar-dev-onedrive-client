namespace AStar.Dev.OneDrive.Client.Core.Interfaces;

public interface IAuthService
{
    Task SignInAsync(CancellationToken cancellationToken);
    Task SignOutAsync(CancellationToken cancellationToken);
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
    Task<bool> IsUserSignedInAsync(CancellationToken cancellationToken);
}
