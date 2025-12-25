# Network Connection Error Handling

## Overview
Enhanced error handling for `IOException` errors, specifically "connection forcibly closed by remote host" errors that can occur during OneDrive API downloads.

## Error: `System.IO.IOException: Unable to read data from the transport connection`

### Root Causes
This error typically occurs when:
1. **Network instability**: Temporary network disruption or packet loss
2. **Server-side timeout**: OneDrive server closes connection due to inactivity
3. **Firewall/Proxy interference**: Corporate firewalls interrupting long-running connections
4. **Large file transfers**: Connection timeout during large file downloads
5. **Rate limiting**: OneDrive throttling requests

### Solution Implemented

#### 1. Enhanced Retry Policies (Multiple Layers)

**TransferService Retry Policy:**
- Handles `HttpRequestException` and `IOException`
- Exponential backoff: Base delay × 2^retry_attempt
- Default: 3 retries with increasing delays
- Configuration: `appsettings.json` ? `UiSettings.SyncSettings.MaxRetries` & `RetryBaseDelayMs`

**HttpClient Polly Policies:**
- **Retry Policy**: Handles transient HTTP errors, 5xx errors, 429 rate limiting, IOExceptions
- **Circuit Breaker**: Opens after 5 consecutive failures, prevents cascade failures
- **Timeout**: 5-minute overall request timeout

#### 2. Improved Error Logging

**GraphClientWrapper Logging:**
```csharp
catch (IOException ex)
{
    _logger.LogError(ex, "Network I/O error downloading DriveItemId: {DriveItemId}. 
        This may indicate a connection reset or timeout. Message: {Message}", 
        driveItemId, ex.Message);
    throw;
}
```

**TransferService Retry Logging:**
```
[Network I/O] Retry 1/3 after 500ms. Error: Unable to read data from the transport connection
[Network I/O] Retry 2/3 after 1000ms. Error: Unable to read data from the transport connection
[Network I/O] Retry 3/3 after 2000ms. Error: Unable to read data from the transport connection
```

**Final Failure Logging:**
```
Failed to download MyFile.pdf after 3 retries. Network connection error - connection was forcibly closed or timed out
```

#### 3. HttpClient Configuration

```csharp
.ConfigureHttpClient(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5); // Overall request timeout
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler 
{ 
    AllowAutoRedirect = true,
    MaxConnectionsPerServer = 10 // Optimize connection pooling
})
```

### Retry Strategy

**Exponential Backoff Calculation:**
```
Retry 1: BaseDelay × 2^1 = 500ms × 2 = 1000ms
Retry 2: BaseDelay × 2^2 = 500ms × 4 = 2000ms
Retry 3: BaseDelay × 2^3 = 500ms × 8 = 4000ms
```

**Total Retry Time:** ~7 seconds (configurable)

### Configuration

**appsettings.json:**
```json
{
  "UiSettings": {
    "SyncSettings": {
      "MaxRetries": 3,
      "RetryBaseDelayMs": 500,
      "MaxParallelDownloads": 4
    }
  }
}
```

**Recommendations:**
- **Slow/Unstable Networks**: Increase `MaxRetries` to 5, `RetryBaseDelayMs` to 1000
- **Fast Networks**: Keep defaults (3 retries, 500ms base)
- **Large Files**: Consider reducing `MaxParallelDownloads` to 2-3 to reduce contention

### Error Detection & Classification

The system now detects and classifies network errors:

```csharp
var isNetworkError = ex is IOException || 
                     (ex is HttpRequestException hre && hre.InnerException is IOException);
```

**Logged as:**
- `[Network I/O]` - Connection-related errors
- `[HttpRequestException]` - HTTP protocol errors
- `[Other]` - Unexpected errors

### What Happens When Error Occurs

1. **First Attempt**: File download starts
2. **IOException**: Connection forcibly closed
3. **Retry 1** (after 1s): Logged as `[Network I/O] Retry 1/3`
4. **Retry 2** (after 2s): Logged as `[Network I/O] Retry 2/3`
5. **Retry 3** (after 4s): Logged as `[Network I/O] Retry 3/3`
6. **Success**: File downloads, marked as `SyncState.Downloaded`
7. **OR Failure**: Logged with detailed error, file marked as `TransferStatus.Failed`

### Monitoring Network Issues

**Enable Debug Logging:**
```json
{
  "Logging": {
    "LogLevel": {
      "AStar.Dev.OneDrive.Client.Infrastructure.Graph": "Debug",
      "AStar.Dev.OneDrive.Client.Services.TransferService": "Debug"
    }
  }
}
```

**Look for patterns:**
```
DEBUG: Requesting download for DriveItemId: ABC123
DEBUG: Download stream acquired for DriveItemId: ABC123
ERROR: Network I/O error downloading DriveItemId: ABC123
WARN: [Network I/O] Retry 1/3 after 1000ms
```

### Advanced Troubleshooting

**If errors persist after retries:**

1. **Check Network Stability:**
   ```powershell
   ping graph.microsoft.com -t
   ```

2. **Check Proxy/Firewall:**
   - Verify HTTPS (443) access to `*.graph.microsoft.com`
   - Check corporate proxy settings

3. **Test OneDrive Connectivity:**
   ```powershell
   Test-NetConnection -ComputerName graph.microsoft.com -Port 443
   ```

4. **Reduce Parallel Downloads:**
   - Lower `MaxParallelDownloads` from 4 to 2
   - Reduces network contention

5. **Increase Timeouts:**
   - Increase `RetryBaseDelayMs` for slower connections
   - Increase `MaxRetries` for unstable networks

### CircuitBreaker Pattern

If 5 consecutive failures occur:
- Circuit opens for 30 seconds
- All requests fail fast during this period
- Prevents overwhelming unstable network
- Automatically resets after cool-down

**Circuit Breaker Log Messages:**
```
Circuit breaker opened for 30s due to IOException
... (30 seconds later)
Circuit breaker reset
```

### Related Files
- `src/AStar.Dev.OneDrive.Client.Services/TransferService.cs` - Retry policy and error handling
- `src/AStar.Dev.OneDrive.Client.Infrastructure/Graph/GraphClientWrapper.cs` - HTTP client and logging
- `src/AStar.Dev.OneDrive.Client.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs` - HttpClient configuration

### Version
Added: Enhanced Network Error Handling
Last Updated: 2024
