using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AStar.Dev.OneDrive.Client.Infrastructure.HealthChecks;

/// <summary>
/// Health check for Microsoft Graph API connectivity.
/// </summary>
public sealed class GraphApiHealthCheck : IHealthCheck
{
    private readonly IAuthService _authService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphApiHealthCheck"/> class.
    /// </summary>
    /// <param name="authService">The authentication service.</param>
    public GraphApiHealthCheck(IAuthService authService) => _authService = authService;

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if user is signed in
            if (!_authService.IsSignedIn)
            {
                return HealthCheckResult.Degraded("User is not authenticated with Microsoft Graph API");
            }

            // Try to get access token (validates auth is working)
            var token = await _authService.GetAccessTokenAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                { "authenticated", true },
                { "tokenAcquired", !string.IsNullOrEmpty(token) }
            };

            return HealthCheckResult.Healthy("Microsoft Graph API authentication is configured", data);
        }
        catch (HttpRequestException ex)
        {
            return HealthCheckResult.Unhealthy("Microsoft Graph API is not accessible", ex, new Dictionary<string, object>
            {
                { "errorType", "NetworkError" }
            });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Health check failed with unexpected error", ex);
        }
    }
}
