# Enhanced Download Logging

## Overview
Comprehensive logging has been added to the download process to diagnose issues where downloads stop unexpectedly or files are being re-downloaded.

## Log Levels & What They Mean

### Information Level (Always Visible)
These logs appear in normal operation and provide high-level progress:

```
Processing pending downloads
Found {TotalPending} pending downloads (batch size: {BatchSize})
Processing batch {BatchNumber} with {ItemCount} files
Batch {BatchNumber} complete: {Processed}/{Total} files downloaded so far
Downloaded {Path} ({SizeKB} KB)
Completed downloads: {Processed} files, {TotalMB} MB in {ElapsedSec}s ({SpeedMBps} MB/s)
```

**Special Cases:**
- `No pending downloads found - sync complete` - Indicates no files need downloading
- `No more items in batch - all downloads complete` - Normal completion
- `Download process cancelled after {Processed}/{Total} files` - User or system cancelled

### Debug Level (Detailed Diagnostics)
Enable debug logging to see detailed operation flow:

```
Repository stats: {TotalItems} total items, {TotalFiles} files, {Downloaded} already downloaded
GetPendingDownloadsAsync(pageSize={PageSize}, offset={Offset}): returning {Count} items
GetPendingDownloadCountAsync: {Count} pending downloads
Fetching batch {PageNumber} (offset: {Offset})
Starting parallel download of {Count} files
Starting download: {Path} ({SizeKB} KB)
Downloading content for: {Path}
Writing file to disk: {Path}
Marking file as downloaded: {Path}
```

### Error Level
Critical issues that prevent downloads:

```
Download failed for {Path} (DriveItemId: {DriveItemId})
Failed to download {Path} after all retries
```

## Troubleshooting Common Scenarios

### Scenario 1: Downloads Stop Immediately
**Symptoms:** Sync starts but immediately reports complete with 0 files

**Check logs for:**
```
No pending downloads found - sync complete
```

**Diagnosis:** Repository correctly identifies all files as already downloaded

**Verify with debug logs:**
```
Repository stats: 150 total items, 145 files, 145 already downloaded
GetPendingDownloadCountAsync: 0 pending downloads
```

**Root Cause:** Files were previously downloaded and correctly marked as `SyncState.Downloaded`

### Scenario 2: Downloads Stop Mid-Process
**Symptoms:** Downloads some files then stops

**Check logs for:**
```
Processing batch 3 with 50 files
No more items in batch - all downloads complete
Completed downloads: 150 files...
```

**Diagnosis:** Normal completion - all pending files were downloaded

**Alternative - Cancellation:**
```
Download process cancelled after 75/150 files
```

**Root Cause:** User cancelled or application shutdown initiated

### Scenario 3: Files Being Re-downloaded Every Sync
**Symptoms:** Same files download repeatedly on each sync

**Check debug logs for:**
```
Repository stats: 150 total items, 145 files, 0 already downloaded
GetPendingDownloadCountAsync: 145 pending downloads
```

**Diagnosis:** Files not being marked as downloaded in database

**Action:** Check for errors during file marking:
```
ERROR: Failed to mark file as downloaded: {Path}
```

**Root Cause:** Database write failure or `MarkLocalFileStateAsync` not being called

### Scenario 4: Batch Processing Issues
**Symptoms:** Downloads process in batches but some batches are empty

**Check debug logs for:**
```
Fetching batch 1 (offset: 0)
GetPendingDownloadsAsync(pageSize=100, offset=0): returning 100 items
Processing batch 1 with 100 files
Batch 1 complete: 100/145 files downloaded so far

Fetching batch 2 (offset: 100)
GetPendingDownloadsAsync(pageSize=100, offset=100): returning 45 items
Processing batch 2 with 45 files
Batch 2 complete: 145/145 files downloaded so far

Fetching batch 3 (offset: 200)
GetPendingDownloadsAsync(pageSize=100, offset=200): returning 0 items
No more items in batch - all downloads complete
```

**Diagnosis:** Normal pagination - final batch check returns 0 items

## Enabling Debug Logging

### In appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "AStar.Dev.OneDrive.Client.Services.TransferService": "Debug",
      "AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories": "Debug"
    }
  }
}
```

### Temporary Console Override
```csharp
// In Program.cs or startup
builder.Services.Configure<LoggerFilterOptions>(options =>
{
    options.MinLevel = LogLevel.Debug;
});
```

## Performance Metrics in Logs

### Summary Statistics
At completion, you'll see:
```
Completed downloads: 150 files, 2,457.32 MB in 45.23s (54.32 MB/s)
```

**Metrics:**
- **Files**: Total files successfully downloaded
- **Total MB**: Actual bytes transferred (not file sizes)
- **Elapsed Time**: Wall-clock time from start to finish
- **Speed**: Average transfer rate including all overhead

### Batch Statistics
```
Estimated total download size: 2,500.00 MB (avg file size: 17.24 MB)
```

**Note:** Estimated from first batch average - actual size may vary

## Repository Query Efficiency

The download query now includes:
1. **Folder filtering**: `!d.IsFolder` - Excludes folders
2. **Deletion filtering**: `!d.IsDeleted` - Excludes deleted items
3. **Sync state filtering**: Excludes items with `SyncState.Downloaded` or `SyncState.Uploaded` in LocalFiles table

This prevents re-downloading files that are already present locally.

## Related Files
- `src/AStar.Dev.OneDrive.Client.Services/TransferService.cs` - Download orchestration
- `src/AStar.Dev.OneDrive.Client.Infrastructure/Data/Repositories/EfSyncRepository.cs` - Query logic

## Version
Added: Phase 4 - Performance & Diagnostics Enhancement
Last Updated: 2024
