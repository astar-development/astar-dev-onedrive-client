# Retry Policies and Resilience

## Overview

The OneDrive Sync application uses **Polly** (via Microsoft.Extensions.Http.Polly) to implement resilience patterns for HTTP operations against the Microsoft Graph API.

## Why Retry Policies?

Microsoft Graph API can experience:
- **Transient network failures** (connection timeouts, DNS failures)
- **Rate limiting** (429 Too Many Requests)
- **Service unavailability** (503 Service Unavailable)
- **Temporary server errors** (500 Internal Server Error, 502 Bad Gateway)

Without retry policies, these transient failures would immediately fail user operations.

## Implemented Policies

### 1. Retry Policy with Exponential Backoff

**Location:** `InfrastructureServiceCollectionExtensions.GetRetryPolicy()`

**Strategy:**
- **Retry Count**: 3 attempts
- **Backoff**: Exponential (2^n seconds: 2s, 4s, 8s)
- **Triggers**:
  - Network failures (HttpRequestException)
  - 5xx server errors (500-599)
  - 408 Request Timeout
  - 429 Too Many Requests (rate limiting)

**Implementation:**
```csharp
private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()  // 5xx, 408, network failures
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
```

**Retry Timeline:**
```
Attempt 1: Request fails
          ? Wait 2 seconds
Attempt 2: Request fails
          ? Wait 4 seconds
Attempt 3: Request fails
          ? Wait 8 seconds
Attempt 4: Request fails ? Exception thrown
```

### 2. Circuit Breaker Policy

**Location:** `InfrastructureServiceCollectionExtensions.GetCircuitBreakerPolicy()`

**Strategy:**
- **Failure Threshold**: 5 consecutive failures
- **Break Duration**: 30 seconds
- **States**: Closed ? Open ? Half-Open ? Closed

**Implementation:**
```csharp
private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30));
```

**Circuit States:**

```
???????????????????????????????????????????????????????????????
? CLOSED (Normal Operation)                                   ?
? ? 5 consecutive failures                                    ?
???????????????????????????????????????????????????????????????
? OPEN (Fast Fail)                                            ?
? • All requests fail immediately with BrokenCircuitException ?
? • No requests sent to Graph API                             ?
? ? After 30 seconds                                          ?
???????????????????????????????????????????????????????????????
? HALF-OPEN (Testing)                                         ?
? • Next request allowed through                              ?
? • Success ? Circuit CLOSED                                  ?
? • Failure ? Circuit OPEN again                              ?
???????????????????????????????????????????????????????????????
```

## Policy Execution Order

Policies are applied in the order they are added:

```csharp
services.AddHttpClient<IGraphClient, GraphClientWrapper>()
    .AddPolicyHandler(GetRetryPolicy())           // Inner policy
    .AddPolicyHandler(GetCircuitBreakerPolicy()); // Outer policy
```

**Execution Flow:**
```
Request
  ?
Circuit Breaker (checks if circuit is open)
  ?
Retry Policy (handles failures with backoff)
  ?
HTTP Request
  ?
Response (or exception)
  ?
Retry Policy (evaluates retry conditions)
  ?
Circuit Breaker (tracks failures)
  ?
Return to caller
```

## Handled Scenarios

### Scenario 1: Transient Network Failure

```
User Action: Click "Initial Sync"
  ?
GraphClient.GetDriveDeltaPageAsync()
  ?
HTTP Request ? Network timeout (HttpRequestException)
  ?
Retry Policy: Wait 2 seconds, retry
  ?
HTTP Request ? Success!
  ?
Return DeltaPage to caller

Result: User sees successful sync (no error shown)
```

### Scenario 2: Rate Limiting (429)

```
Request 1 ? 429 Too Many Requests
  ?
Retry Policy: Wait 2 seconds, retry
  ?
Request 2 ? 429 Too Many Requests
  ?
Retry Policy: Wait 4 seconds, retry
  ?
Request 3 ? 200 OK
  ?
Return response

Result: Automatic backoff respects rate limits
```

### Scenario 3: Service Outage (Circuit Breaker)

```
Requests 1-5 ? All fail (500 Internal Server Error)
  ?
Circuit Breaker: OPEN (after 5th failure)
  ?
Requests 6-N ? BrokenCircuitException (no HTTP calls made)
  ?
Wait 30 seconds...
  ?
Circuit Breaker: HALF-OPEN
  ?
Next request ? Success
  ?
Circuit Breaker: CLOSED (normal operation resumed)

Result: Fast failure during outage, automatic recovery when service returns
```

## Configuration Options

### Customizing Retry Count

```csharp
// In GetRetryPolicy(), change retryCount
.WaitAndRetryAsync(
    retryCount: 5,  // Change from 3 to 5 attempts
    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
```

### Customizing Backoff Strategy

```csharp
// Linear backoff (1s, 2s, 3s)
sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(retryAttempt)

// Fixed delay (2s, 2s, 2s)
sleepDurationProvider: _ => TimeSpan.FromSeconds(2)

// Exponential with jitter (recommended for high concurrency)
sleepDurationProvider: retryAttempt => 
    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + 
    TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000))
```

### Customizing Circuit Breaker

```csharp
.CircuitBreakerAsync(
    handledEventsAllowedBeforeBreaking: 10,  // More lenient
    durationOfBreak: TimeSpan.FromMinutes(1)) // Longer break
```

## Monitoring and Observability

### Future: Add Logging to Policies

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

### Polly Context for Request Tracking

```csharp
var context = new Context($"GraphAPI-{operationName}");
await policy.ExecuteAsync(ctx => httpClient.SendAsync(request, ct), context);
```

## Testing Retry Policies

### Unit Test: Verify Retry Behavior

```csharp
[Fact]
public async Task GraphClient_RetriesOnTransientFailure()
{
    // Arrange
    var handler = new MockHttpMessageHandler();
    handler.AddResponse(HttpStatusCode.ServiceUnavailable);  // First call fails
    handler.AddResponse(HttpStatusCode.OK);                  // Second call succeeds

    var httpClient = new HttpClient(handler);
    var policy = GetRetryPolicy();
    var client = new HttpClient(new PolicyHttpMessageHandler(policy) { InnerHandler = handler });

    // Act
    var response = await client.GetAsync("https://graph.microsoft.com/v1.0/me");

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    handler.RequestCount.ShouldBe(2);  // Verify retry occurred
}
```

### Integration Test: Simulate Rate Limiting

```csharp
[Fact]
public async Task GraphClient_HandlesRateLimiting()
{
    // Requires real HTTP calls with mocked 429 responses
    // Or use WireMock.Net for HTTP mocking
}
```

## Best Practices

### 1. **Choose Appropriate Retry Count**
- **3-5 retries** for transient failures
- **1-2 retries** for user-facing operations (avoid long waits)
- **No retries** for non-idempotent operations (POST creates)

### 2. **Use Exponential Backoff**
- Prevents "retry storms" that overwhelm the service
- Gives service time to recover
- Respects rate limits

### 3. **Add Jitter for High Concurrency**
- Multiple clients retrying simultaneously cause "thundering herd"
- Random jitter (±100-1000ms) spreads out retry attempts

### 4. **Circuit Breaker for Fast Failure**
- Don't waste resources on known-bad endpoints
- Fail fast during outages
- Automatic recovery when service returns

### 5. **Monitor Retry Metrics**
- Track retry counts, delays, circuit state
- Alert on high retry rates (indicates service issues)
- Log circuit breaker events

## Graph API-Specific Considerations

### Retry-After Header

Microsoft Graph API includes `Retry-After` header with 429 responses:

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 120
```

**Future Enhancement: Respect Retry-After**
```csharp
.WaitAndRetryAsync(
    retryCount: 3,
    sleepDurationProvider: (retryAttempt, outcome, context) =>
    {
        // Check for Retry-After header
        if (outcome?.Result?.Headers.RetryAfter?.Delta is TimeSpan retryAfter)
        {
            return retryAfter;
        }
        
        // Default exponential backoff
        return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
    });
```

### Idempotent vs Non-Idempotent Operations

**Safe to Retry:**
- ? GET requests (delta queries, file downloads)
- ? DELETE requests (idempotent)
- ? PUT requests (idempotent upload chunks)

**Unsafe to Retry (without additional logic):**
- ? POST requests (create upload session - may create duplicates)

## Alternatives and Extensions

### 1. **Timeout Policy**
```csharp
Policy
    .TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30))
```

### 2. **Bulkhead Isolation**
```csharp
Policy
    .BulkheadAsync<HttpResponseMessage>(maxParallelization: 10, maxQueuingActions: 20)
```

### 3. **Fallback Policy**
```csharp
Policy<HttpResponseMessage>
    .Handle<Exception>()
    .FallbackAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))
```

### 4. **Policy Wrap (Combine Multiple Policies)**
```csharp
var combined = Policy.WrapAsync(
    GetCircuitBreakerPolicy(),
    GetRetryPolicy(),
    GetTimeoutPolicy());
```

## Troubleshooting

### Issue: "Too many retries causing slow UI"

**Solution:** Reduce retry count or add timeout policy
```csharp
.WaitAndRetryAsync(retryCount: 2, ...)
```

### Issue: "Circuit breaker opens too easily"

**Solution:** Increase failure threshold
```csharp
.CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 10, ...)
```

### Issue: "No logs showing retry attempts"

**Solution:** Add `onRetry` callback with logging (see Monitoring section)

## Production Recommendations

1. **Retry Policy**:
   - 3 retries with exponential backoff
   - Add jitter for high concurrency
   - Respect `Retry-After` header

2. **Circuit Breaker**:
   - 5-10 failures threshold
   - 30-60 second break duration
   - Log circuit state changes

3. **Timeout Policy**:
   - 30 second timeout per request
   - Prevents indefinite hangs

4. **Monitoring**:
   - Track retry metrics
   - Alert on circuit breaker events
   - Monitor 429 rate limiting frequency

## Summary

**Current Implementation:**
- ? Retry policy with exponential backoff (3 retries, 2^n seconds)
- ? Circuit breaker (5 failures, 30 second break)
- ? Handles transient errors (5xx, 408, network failures, 429)

**Future Enhancements:**
- ?? Logging in `onRetry` and `onBreak` callbacks
- ?? Respect `Retry-After` header for 429 responses
- ?? Timeout policy (30 second max per request)
- ?? Metrics collection (retry count, circuit state, failure rate)
- ?? Per-operation policy customization

**Benefits:**
- Improved reliability against transient failures
- Automatic backoff for rate limiting
- Fast failure during outages
- Better user experience (no immediate errors for transient issues)
- Production-ready resilience
