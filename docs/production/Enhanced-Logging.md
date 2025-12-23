# Enhanced Logging with Structured Logging

## Overview

The OneDrive Sync application uses **Microsoft.Extensions.Logging** with structured logging for comprehensive diagnostics, debugging, and monitoring.

## Logging Strategy

### Log Levels

| Level | Usage | Examples |
|-------|-------|----------|
| **Trace** | Detailed diagnostic information | Method entry/exit, loop iterations |
| **Debug** | Development debugging | Variable values, conditional branches |
| **Information** | Normal application flow | Sync started, file downloaded, auth successful |
| **Warning** | Unexpected but recoverable | Missing optional config, fallback behavior |
| **Error** | Operation failures | Network timeout, file access denied |
| **Critical** | Application-level failures | Database corruption, unrecoverable state |

### Structured Logging Pattern

Always use structured logging with named parameters:

```csharp
// ? CORRECT - Structured logging with named parameters
_logger.LogInformation("Downloading file {FileName} with size {FileSize} bytes from {DriveItemId}",
    fileName, fileSize, driveItemId);

// ? WRONG - String interpolation loses structure
_logger.LogInformation($"Downloading file {fileName} with size {fileSize} bytes from {driveItemId}");
```

## Logging by Component

### 1. Infrastructure Layer

#### GraphClientWrapper
```csharp
public sealed class GraphClientWrapper : IGraphClient
{
    private readonly IAuthService _auth;
    private readonly HttpClient _http;
    private readonly ILogger<GraphClientWrapper> _logger;

    public GraphClientWrapper(IAuthService auth, HttpClient http, ILogger<GraphClientWrapper> logger)
    {
        _auth = auth;
        _http = http;
        _logger = logger;
    }

    public async Task<DeltaPage> GetDriveDeltaPageAsync(string? deltaOrNextLink, CancellationToken ct)
    {
        _logger.LogDebug("Fetching delta page from {DeltaLink}", deltaOrNextLink ?? "root");
        
        try
        {
            var url = string.IsNullOrEmpty(deltaOrNextLink)
                ? "https://graph.microsoft.com/v1.0/me/drive/root/delta"
                : deltaOrNextLink;

            HttpResponseMessage res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            res.EnsureSuccessStatusCode();

            _logger.LogInformation("Successfully fetched delta page with {ItemCount} items", items.Count);
            return new DeltaPage(items, next, delta);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("Rate limited by Graph API, retry policy will handle");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch delta page from {DeltaLink}", deltaOrNextLink);
            throw;
        }
    }
}
```

### 2. Services Layer

#### TransferService
```csharp
private async Task DownloadFileAsync(DriveItemRecord item, CancellationToken ct)
{
    _logger.LogInformation("Starting download for {FileName} ({FileSize} bytes) to {LocalPath}",
        item.Name, item.Size, item.RelativePath);

    var sw = Stopwatch.StartNew();
    
    try
    {
        await using Stream graphStream = await _graphClient.DownloadDriveItemContentAsync(item.DriveId, ct);
        await _localFileSystem.WriteFileAsync(item.RelativePath, graphStream, ct);
        
        sw.Stop();
        _logger.LogInformation("Downloaded {FileName} in {ElapsedMs}ms at {SpeedMbps:F2} Mbps",
            item.Name, sw.ElapsedMilliseconds, CalculateSpeedMbps(item.Size, sw.Elapsed));
    }
    catch (IOException ex)
    {
        _logger.LogError(ex, "Failed to write file {FileName} to disk: {ErrorMessage}",
            item.Name, ex.Message);
        throw;
    }
}
```

### 3. Polly Retry Policy Logging

```csharp
private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger) =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                var statusCode = outcome.Result?.StatusCode.ToString() ?? "NetworkFailure";
                logger.LogWarning("HTTP request retry {RetryCount} after {DelaySec}s delay due to {StatusCode}",
                    retryCount, timespan.TotalSeconds, statusCode);
            });
```

## Log Scopes for Context

Use log scopes to add contextual information to all logs within a scope:

```csharp
public async Task SyncAsync(CancellationToken ct)
{
    using (_logger.BeginScope(new Dictionary<string, object>
    {
        ["SyncSessionId"] = Guid.NewGuid(),
        ["SyncType"] = "Initial"
    }))
    {
        _logger.LogInformation("Starting initial sync");
        
        // All logs within this scope will include SyncSessionId and SyncType
        await ProcessDeltaAsync(ct);
        await DownloadFilesAsync(ct);
        
        _logger.LogInformation("Initial sync completed");
    }
}
```

## Performance Logging

### Track Operation Duration
```csharp
public async Task<int> ProcessBatchAsync(IReadOnlyList<DriveItemRecord> batch, CancellationToken ct)
{
    using var activity = _logger.BeginScope("ProcessBatch");
    var sw = Stopwatch.StartNew();
    
    _logger.LogDebug("Processing batch of {BatchSize} items", batch.Count);
    
    int processed = 0;
    foreach (var item in batch)
    {
        await ProcessItemAsync(item, ct);
        processed++;
    }
    
    sw.Stop();
    _logger.LogInformation("Processed {ProcessedCount} items in {ElapsedMs}ms ({ItemsPerSec:F2} items/sec)",
        processed, sw.ElapsedMilliseconds, processed / sw.Elapsed.TotalSeconds);
    
    return processed;
}
```

## Error Logging Best Practices

### 1. Include Exception Details
```csharp
try
{
    await OperationAsync();
}
catch (SpecificException ex)
{
    // ? CORRECT - Log exception object, then add context
    _logger.LogError(ex, "Operation failed for {EntityId} with {Reason}",
        entityId, ex.Reason);
    throw;
}
```

### 2. Log at Appropriate Level
```csharp
// Expected errors: Warning
if (!File.Exists(path))
{
    _logger.LogWarning("Configuration file not found at {Path}, using defaults", path);
    return DefaultConfig();
}

// Unexpected errors: Error
catch (UnexpectedConditionException ex)
{
    _logger.LogError(ex, "Unexpected condition in {MethodName}", nameof(ProcessAsync));
    throw;
}

// Fatal errors: Critical
catch (DatabaseCorruptedException ex)
{
    _logger.LogCritical(ex, "Database corruption detected, application cannot continue");
    Environment.Exit(1);
}
```

### 3. Avoid Sensitive Data
```csharp
// ? WRONG - Logs sensitive token
_logger.LogDebug("Auth token: {Token}", accessToken);

// ? CORRECT - Log token characteristics, not value
_logger.LogDebug("Auth token acquired, expires at {ExpiresAt}", expiresAt);
```

## Configuration

### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "AStar.Dev.OneDrive.Client": "Debug",
      "AStar.Dev.OneDrive.Client.Infrastructure.Graph": "Information"
    }
  }
}
```

### Development vs Production

**Development (appsettings.Development.json):**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "AStar.Dev.OneDrive.Client": "Trace"
    }
  }
}
```

**Production:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "AStar.Dev.OneDrive.Client": "Information"
    }
  }
}
```

## Serilog Integration (Future)

For production deployments, consider Serilog for:
- File-based logging with rolling files
- Structured JSON logs
- Cloud logging (Application Insights, Seq, etc.)

```csharp
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/onedrive-sync-.log", 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq("http://localhost:5341"));
```

## Testing with Logging

### Unit Tests
```csharp
[Fact]
public async Task DownloadFile_LogsInformationOnSuccess()
{
    // Arrange
    var mockLogger = Substitute.For<ILogger<TransferService>>();
    var service = new TransferService(mockLogger, ...);

    // Act
    await service.DownloadFileAsync(item, CancellationToken.None);

    // Assert
    mockLogger.Received(1).Log(
        LogLevel.Information,
        Arg.Any<EventId>(),
        Arg.Is<object>(o => o.ToString()!.Contains("Downloaded")),
        null,
        Arg.Any<Func<object, Exception?, string>>());
}
```

### Integration Tests
Use real logger to verify log output:
```csharp
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILogger<MyService>>();
```

## Log Analysis

### Common Queries

**Find all errors in last hour:**
```
LogLevel:Error AND Timestamp:[now-1h TO now]
```

**Track sync performance:**
```
"Initial sync completed" AND ElapsedMs
```

**Identify retry patterns:**
```
"HTTP request retry" AND StatusCode:TooManyRequests
```

## Performance Considerations

1. **Guard expensive operations:**
```csharp
if (_logger.IsEnabled(LogLevel.Debug))
{
    var json = JsonSerializer.Serialize(complexObject);
    _logger.LogDebug("Complex object state: {State}", json);
}
```

2. **Use log levels appropriately** - Trace/Debug disabled in production
3. **Structured parameters are cheap** - Template compilation is cached
4. **Avoid string interpolation** - Loses structure and evaluates eagerly

## Monitoring Integration

### Application Insights (Future)
```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Health Check Logging
```csharp
public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
{
    _logger.LogDebug("Performing {HealthCheckName} health check", context.Registration.Name);
    
    try
    {
        // Perform check
        _logger.LogInformation("{HealthCheckName} is healthy", context.Registration.Name);
        return HealthCheckResult.Healthy();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "{HealthCheckName} failed", context.Registration.Name);
        return HealthCheckResult.Unhealthy(ex.Message, ex);
    }
}
```

## Summary

**Key Principles:**
1. Use structured logging with named parameters
2. Log at appropriate levels (Trace < Debug < Information < Warning < Error < Critical)
3. Include contextual information (file names, IDs, durations)
4. Never log sensitive data (tokens, passwords)
5. Use log scopes for request/operation context
6. Guard expensive logging operations
7. Test logging behavior in unit tests

**Benefits:**
- Easy troubleshooting with detailed context
- Performance monitoring with timing data
- Production monitoring with structured queries
- Debugging support with Trace/Debug levels
- Security audit trail with Information logs
