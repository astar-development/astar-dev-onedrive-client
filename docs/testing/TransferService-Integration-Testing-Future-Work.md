# TransferService Integration Testing - Future Work

## Overview
The `TransferService` class orchestrates file downloads and uploads between OneDrive and local storage with Polly retry policies, bounded concurrency, and progress reporting. While high-value for integration testing, it requires significant infrastructure work to make properly testable.

## Current Blockers

### 1. LocalFileSystemAdapter Architecture
**Issue**: The production `LocalFileSystemAdapter` only accepts a root directory string and uses the real .NET filesystem APIs directly.

```csharp
public sealed class LocalFileSystemAdapter : IFileSystemAdapter
{
    private readonly string _root;
    public LocalFileSystemAdapter(string root) => _root = root;
    // Uses System.IO.File, System.IO.Directory directly
}
```

**Impact**: Cannot use `MockFileSystem` from TestableIO for in-memory testing.

**Options**:
- **Option A**: Refactor `LocalFileSystemAdapter` to accept `IFileSystem` from TestableIO.System.IO.Abstractions
  - **Pros**: Enables in-memory testing, better testability
  - **Cons**: Changes production code for testing, adds dependency
  
- **Option B**: Use real filesystem with temp directories
  - **Pros**: Tests real I/O behavior, no production code changes
  - **Cons**: Slower tests, requires cleanup, potential file locking issues
  
- **Option C**: Create test-specific adapter wrapper
  - **Pros**: Isolates test infrastructure
  - **Cons**: Divergence between test and production paths

**Recommendation**: Option A - Refactor to use IFileSystem (already a project dependency). Update constructor:
```csharp
public LocalFileSystemAdapter(IFileSystem fileSystem, string root)
```

### 2. Entity Constructor Signatures

#### LocalFileRecord
**Current**: 6 parameters (no DriveItemId field)
```csharp
public sealed record LocalFileRecord(
    string Id,
    string RelativePath,
    string? Hash,
    long Size,
    DateTimeOffset LastWriteUtc,
    SyncState SyncState
);
```

**Test Assumption**: 7 parameters with DriveItemId
**Resolution**: Use correct 6-parameter constructor

#### UploadSessionInfo
**Current**: 3 parameters including SessionId
```csharp
public sealed record UploadSessionInfo(string UploadUrl, string SessionId, DateTimeOffset ExpiresAt);
```

**Test Code**: Was missing SessionId parameter
**Resolution**: Include SessionId in test setup:
```csharp
UploadSessionInfo sessionInfo = new("sessionUrl", "sessionId123", DateTimeOffset.UtcNow.AddHours(1));
```

### 3. Missing Repository Methods

**Issue**: `ISyncRepository` does not provide `GetLocalFileByPathAsync(string relativePath, CancellationToken ct)`

**Current Methods**:
- `GetPendingUploadsAsync(int limit, CancellationToken ct)` - returns all pending, no path filter
- `AddOrUpdateLocalFileAsync(LocalFileRecord file, CancellationToken ct)` - upsert only
- `MarkLocalFileStateAsync(string driveItemId, SyncState state, CancellationToken ct)` - uses DriveItemId, not path

**Impact**: Cannot easily verify state changes for specific files in assertions.

**Options**:
- **Option A**: Add `GetLocalFileByPathAsync` to `ISyncRepository` and `EfSyncRepository`
  ```csharp
  Task<LocalFileRecord?> GetLocalFileByPathAsync(string relativePath, CancellationToken ct);
  ```
  - **Pros**: Clean API, reusable
  - **Cons**: Adds method not used in production
  
- **Option B**: Query `AppDbContext.LocalFiles` directly in tests
  ```csharp
  LocalFileRecord? file = await _context.LocalFiles
      .FirstOrDefaultAsync(f => f.RelativePath == "test.txt", ct);
  ```
  - **Pros**: No production code changes
  - **Cons**: Test code accesses internal implementation

**Recommendation**: Option B for integration tests (acceptable to query DbContext directly). If method is needed in production later, add to interface.

### 4. NSubstitute Mocking Complexity

**Issue**: Complex async returns and exception throwing patterns required.

**Download Failure Pattern**:
```csharp
// ? DOESN'T WORK - lambda type mismatch
_mockGraph.DownloadDriveItemContentAsync("item2", Arg.Any<CancellationToken>())
    .Returns(callInfo => Task.FromException<Stream>(new IOException("Network error")));

// ? WORKS - Use NSubstitute .When().Do()
_mockGraph.When(x => x.DownloadDriveItemContentAsync("item2", Arg.Any<CancellationToken>()))
    .Do(x => throw new IOException("Network error"));
```

**Upload Failure Pattern**:
```csharp
// ? Correct approach
_mockGraph.When(x => x.CreateUploadSessionAsync(Arg.Any<string>(), "fail.txt", Arg.Any<CancellationToken>()))
    .Do(x => throw new HttpRequestException("Upload failed"));
```

### 5. TransferLog Assertion Complexity

**Issue**: `TransferService` logs multiple `TransferLog` entries per operation:
1. `TransferStatus.InProgress` at start
2. `TransferStatus.Success` or `TransferStatus.Failed` at completion

**Impact**: Simple count assertions like `logs.Count.ShouldBe(2)` are fragile.

**Better Approach**:
```csharp
List<TransferLog> logs = await GetAllTransferLogsAsync();
logs.ShouldContain(log => log.Status == TransferStatus.Success && log.Type == TransferType.Download);
logs.ShouldContain(log => log.Status == TransferStatus.InProgress);
```

### 6. Polly Retry Policy Testing

**Challenge**: `TransferService` uses Polly retry policies with exponential backoff:
```csharp
_retryPolicy = Policy.Handle<Exception>()
    .WaitAndRetryAsync(
        settings.UiSettings.SyncSettings.MaxRetries,
        i => TimeSpan.FromMilliseconds(settings.UiSettings.SyncSettings.RetryBaseDelayMs * Math.Pow(2, i)),
        (ex, ts, retryCount, ctx) => _logger.LogWarning(ex, "Retry {Retry} after {Delay}ms", retryCount, ts.TotalMilliseconds)
    );
```

**Testing Considerations**:
- Verify retry attempts count
- Verify exponential backoff timing
- Verify eventual success after transient failures
- Verify final failure after max retries exhausted

**Recommendation**: Create separate test class `TransferServiceRetryPolicyShould` focused on Polly behavior.

### 7. Concurrency Testing

**Challenge**: Testing `SemaphoreSlim` bounded concurrency requires timing-sensitive tests.

**Current Test Pattern** (from incomplete tests):
```csharp
int concurrentCount = 0;
int maxConcurrent = 0;

_mockGraph.DownloadDriveItemContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
    .Returns(async callInfo =>
    {
        Interlocked.Increment(ref concurrentCount);
        int current = concurrentCount;
        if(current > maxConcurrent)
            maxConcurrent = current;
        
        await Task.Delay(50); // Simulate work
        Interlocked.Decrement(ref concurrentCount);
        return new MemoryStream("Test"u8.ToArray());
    });
```

**Issues**:
- Timing-dependent (50ms delay may not be sufficient)
- Race conditions possible
- Flaky test potential

**Better Approach**: Use `TaskCompletionSource` for deterministic control:
```csharp
List<TaskCompletionSource<Stream>> taskSources = [];
_mockGraph.DownloadDriveItemContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
    .Returns(callInfo =>
    {
        TaskCompletionSource<Stream> tcs = new();
        taskSources.Add(tcs);
        return tcs.Task;
    });

// Complete tasks manually to control timing
```

## Recommended Implementation Plan

### Phase 1: Infrastructure Changes
1. ? Refactor `LocalFileSystemAdapter` to accept `IFileSystem` parameter
2. ? Add `GetLocalFileByPathAsync` to `ISyncRepository` (or decide to use DbContext directly in tests)
3. ? Create test helper: `Task<List<TransferLog>> GetAllTransferLogsAsync()`

### Phase 2: Core Tests (High Priority)
4. ? Single file download success
5. ? Multiple file downloads (batch processing)
6. ? Single file upload success
7. ? Large file upload (chunked upload validation)
8. ? Progress event emission (downloads and uploads)
9. ? Empty pending downloads/uploads (no-op scenarios)

### Phase 3: Error Handling Tests (High Priority)
10. ? Download failure - logs error, continues processing other files
11. ? Upload failure - logs error, leaves file in PendingUpload state
12. ? File not found during upload
13. ? Network timeout simulation

### Phase 4: Advanced Scenarios (Medium Priority)
14. ? Batch size respect (process 25 items in batches of 10)
15. ? Concurrent download limit enforcement (MaxParallelDownloads)
16. ? Multiple progress subscribers
17. ?? Retry policy behavior (separate test class recommended)
18. ?? Cancellation token respect

### Phase 5: Edge Cases (Low Priority)
19. ?? Extremely large files (>1GB, chunking edge cases)
20. ?? Unicode/special characters in filenames
21. ?? Path traversal security validation

## Test Count Estimate
- **Phase 2**: 6 tests
- **Phase 3**: 4 tests
- **Phase 4**: 5 tests
- **Phase 5**: 3 tests
- **Total**: ~18 integration tests

## Alternative: End-to-End Testing

**Consideration**: Given the complexity, `TransferService` may be better suited for end-to-end tests with a real Graph API sandbox or recorded HTTP interactions (VCR pattern).

**Pros**:
- Tests real HTTP behavior
- Validates actual Graph API integration
- No mock complexity

**Cons**:
- Slower execution
- Requires Graph API credentials or recordings
- More brittle (external dependency)

## Current Status

? **SyncEngine**: 11 integration tests completed and passing
- Full sync with pagination
- Incremental sync
- Delta token management
- Progress events
- Error handling

? **TransferService**: 0 integration tests (infrastructure blockers)
- Requires refactoring per Phase 1
- Estimated 2-4 hours implementation time after infrastructure work

## References
- Production code: `src/AStar.Dev.OneDrive.Client.Services/TransferService.cs`
- Interface: `src/AStar.Dev.OneDrive.Client.Services/ITransferService.cs`
- Related: `src/AStar.Dev.OneDrive.Client.Infrastructure/FileSystem/LocalFileSystemAdapter.cs`
- Test project: `test/AStar.Dev.OneDrive.Client.Services.Tests.Integration/`

---

**Last Updated**: 2025-01-XX  
**Status**: Documented for future implementation  
**Priority**: Medium (good test coverage exists at other layers)
