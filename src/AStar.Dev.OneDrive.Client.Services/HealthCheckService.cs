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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health report containing status of all checks.</returns>
    Task<HealthReport> GetHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the health status of a specific check by name.
    /// </summary>
    /// <param name="checkName">Name of the health check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health check result or null if not found.</returns>
    Task<HealthCheckResult?> GetHealthCheckAsync(string checkName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of health check service wrapper.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ApplicationHealthCheckService"/> class.
/// </remarks>
/// <param name="healthCheckService">The underlying health check service.</param>
public sealed class ApplicationHealthCheckService(HealthCheckService healthCheckService) : IHealthCheckService
{

    /// <inheritdoc/>
    public async Task<HealthReport> GetHealthAsync(CancellationToken cancellationToken = default)
        => await healthCheckService.CheckHealthAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<HealthCheckResult?> GetHealthCheckAsync(string checkName, CancellationToken cancellationToken = default)
    {
        HealthReport report = await healthCheckService.CheckHealthAsync(cancellationToken);
        return !report.Entries.TryGetValue(checkName, out HealthReportEntry entry)
                ? null
                : UpdateStatus(entry);
    }

    private static HealthCheckResult UpdateStatus(HealthReportEntry entry) => entry.Status == HealthStatus.Healthy
                    ? HealthCheckResult.Healthy(entry.Description, entry.Data)
                    : HealthCheckResult.Unhealthy(entry.Description, entry.Exception, entry.Data);
}
