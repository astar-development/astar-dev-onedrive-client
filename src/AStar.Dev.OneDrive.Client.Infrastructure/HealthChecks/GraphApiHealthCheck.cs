using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AStar.Dev.OneDrive.Client.Infrastructure.HealthChecks;

/// <summary>
/// Health check for Microsoft Graph API connectivity.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GraphApiHealthCheck"/> class.
/// </remarks>
/// <param name="authService">The authentication service.</param>
public sealed class GraphApiHealthCheck(IAuthService authService) : IHealthCheck
{

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var signedIn = await authService.IsUserSignedInAsync(cancellationToken);
            if(!signedIn)
            {
                return HealthCheckResult.Degraded("User is not authenticated with Microsoft Graph API");
            }

            var token = await authService.GetAccessTokenAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                { "authenticated", true },
                { "tokenAcquired", !string.IsNullOrEmpty(token) }
            };

            return HealthCheckResult.Healthy("Microsoft Graph API authentication is configured", data);
        }
        catch(HttpRequestException ex)
        {
            return HealthCheckResult.Unhealthy("Microsoft Graph API is not accessible", ex, new Dictionary<string, object>
            {
                { "errorType", "NetworkError" }
            });
        }
        catch(Exception ex)
        {
            return HealthCheckResult.Unhealthy("Health check failed with unexpected error", ex);
        }
    }
}
