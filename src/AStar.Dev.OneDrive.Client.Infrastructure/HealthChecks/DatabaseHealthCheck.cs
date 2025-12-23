using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AStar.Dev.OneDrive.Client.Infrastructure.HealthChecks;

/// <summary>
/// Health check for database connectivity and basic operations.
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseHealthCheck"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public DatabaseHealthCheck(AppDbContext dbContext) => _dbContext = dbContext;

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test database connection by executing a simple query
            _ = await _dbContext.Database.CanConnectAsync(cancellationToken);
            
            // Check if database is accessible and has expected tables
            var deltaTokenCount = await _dbContext.DeltaTokens.CountAsync(cancellationToken);
            
            var data = new Dictionary<string, object>
            {
                { "deltaTokenCount", deltaTokenCount },
                { "connectionState", "connected" }
            };

            return HealthCheckResult.Healthy("Database is accessible and operational", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is not accessible", ex);
        }
    }
}
