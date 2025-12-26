using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AStar.Dev.OneDrive.Client.Infrastructure.HealthChecks;

/// <summary>
/// Health check for database connectivity and basic operations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DatabaseHealthCheck"/> class.
/// </remarks>
/// <param name="dbContext">The database context.</param>
public sealed class DatabaseHealthCheck(AppDbContext dbContext) : IHealthCheck
{
    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test database connection by executing a simple query
            _ = await dbContext.Database.CanConnectAsync(cancellationToken);

            // Check if database is accessible and has expected tables
            var deltaTokenCount = await dbContext.DeltaTokens.CountAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                { "deltaTokenCount", deltaTokenCount },
                { "connectionState", "connected" }
            };

            return HealthCheckResult.Healthy("Database is accessible and operational", data);
        }
        catch(Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is not accessible", ex);
        }
    }
}
