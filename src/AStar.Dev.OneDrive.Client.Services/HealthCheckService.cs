using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AStar.Dev.OneDrive.Client.Services;

/// <summary>
/// Service for checking application health status.
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Gets the current health status of all registered health checks.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Health report containing status of all checks.</returns>
    Task<HealthReport> GetHealthAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the health status of a specific check by name.
    /// </summary>
    /// <param name="checkName">Name of the health check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Health check result or null if not found.</returns>
    Task<HealthCheckResult?> GetHealthCheckAsync(string checkName, CancellationToken ct = default);
}

/// <summary>
/// Implementation of health check service wrapper.
/// </summary>
public sealed class ApplicationHealthCheckService : IHealthCheckService
{
    private readonly HealthCheckService _healthCheckService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationHealthCheckService"/> class.
    /// </summary>
    /// <param name="healthCheckService">The underlying health check service.</param>
    public ApplicationHealthCheckService(HealthCheckService healthCheckService) => _healthCheckService = healthCheckService;

    /// <inheritdoc/>
    public async Task<HealthReport> GetHealthAsync(CancellationToken ct = default) 
        => await _healthCheckService.CheckHealthAsync(ct);

        /// <inheritdoc/>
        public async Task<HealthCheckResult?> GetHealthCheckAsync(string checkName, CancellationToken ct = default)
        {
            HealthReport report = await _healthCheckService.CheckHealthAsync(ct);
            if (!report.Entries.TryGetValue(checkName, out HealthReportEntry entry))
            {
                return null;
            }

            return entry.Status == HealthStatus.Healthy
                ? HealthCheckResult.Healthy(entry.Description, entry.Data)
                : HealthCheckResult.Unhealthy(entry.Description, entry.Exception, entry.Data);
        }
    }
