namespace App.Core.Interfaces;

public interface IAuthService
{
    Task SignInAsync(CancellationToken ct);
    Task SignOutAsync(CancellationToken ct);
    Task<string> GetAccessTokenAsync(CancellationToken ct);
    bool IsSignedIn { get; }
}
