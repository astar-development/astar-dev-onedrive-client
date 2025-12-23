# Health Checks Implementation

## Overview

The OneDrive Sync application now includes comprehensive health checks to monitor application health and dependencies.

## Health Check Components

### 1. DatabaseHealthCheck
Monitors SQLite database connectivity and basic operations.

**Checks:**
- Database connection state
- Ability to query database tables
- Delta token count (indicates database is populated)

**Status:**
- **Healthy**: Database is accessible and operational
- **Unhealthy**: Database connection failed or query error

### 2. GraphApiHealthCheck
Monitors Microsoft Graph API authentication status.

**Checks:**
- User authentication state (`IsSignedIn`)
- Ability to acquire access tokens
- Token validity

**Status:**
- **Healthy**: User is authenticated and can acquire tokens
- **Degraded**: User is not signed in
- **Unhealthy**: Authentication errors or token acquisition failures

## Usage

### In Code

```csharp
// Inject IHealthCheckService into your class
public class MyService
{
    private readonly IHealthCheckService _healthCheck;
    
    public MyService(IHealthCheckService healthCheck)
    {
        _healthCheck = healthCheck;
    }
    
    public async Task CheckSystemHealthAsync()
    {
        // Get overall health report
        HealthReport report = await _healthCheck.GetHealthAsync();
        
        Console.WriteLine($"Overall Status: {report.Status}");
        foreach (var entry in report.Entries)
        {
            Console.WriteLine($"{entry.Key}: {entry.Value.Status}");
        }
        
        // Check specific health check
        HealthCheckResult? dbHealth = await _healthCheck.GetHealthCheckAsync("database");
        if (dbHealth?.Status == HealthStatus.Healthy)
        {
            Console.WriteLine("Database is healthy!");
        }
    }
}
```

### Health Status Values

- **Healthy**: Component is fully operational
- **Degraded**: Component is operational but with reduced functionality
- **Unhealthy**: Component has failed or is not operational

## Integration with UI

The health checks can be integrated into the MainWindow to display system status:

```csharp
// In MainWindowViewModel
private async Task UpdateHealthStatusAsync()
{
    HealthReport report = await _healthCheckService.GetHealthAsync();
    
    DatabaseStatus = report.Entries["database"].Status.ToString();
    GraphApiStatus = report.Entries["graph_api"].Status.ToString();
    
    OverallHealthy = report.Status == HealthStatus.Healthy;
}
```

## Future Enhancements

1. **Periodic Health Monitoring**: Background service to check health every N minutes
2. **Health Status UI Indicator**: Visual indicator in main window (green/yellow/red)
3. **Health Check Logging**: Automatic logging of health check failures
4. **Additional Checks**:
   - File system adapter health
   - Network connectivity
   - Disk space availability
   - Sync service state

## Testing

Health checks are designed to be testable:

```csharp
[Fact]
public async Task DatabaseHealthCheck_WhenDatabaseAccessible_ReturnsHealthy()
{
    // Arrange
    var dbContext = CreateInMemoryDbContext();
    var healthCheck = new DatabaseHealthCheck(dbContext);
    
    // Act
    var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());
    
    // Assert
    result.Status.ShouldBe(HealthStatus.Healthy);
}
```

## Dependencies

- **Microsoft.Extensions.Diagnostics.HealthChecks** 10.0.1
- Registered in `InfrastructureServiceCollectionExtensions.AddInfrastructure()`
- Service wrapper registered in `ServiceCollectionExtensions.AddSyncServices()`
