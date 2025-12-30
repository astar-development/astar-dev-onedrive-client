using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Services;

/// <summary>
/// Scans local files and updates sync state in the repository.
/// </summary>
public interface ILocalFileScanner
{
    /// <summary>
    /// Scans local files, updates sync state, and returns summary statistics.
    /// </summary>
    Task<(int processedCount, int newFilesCount, int modifiedFilesCount)> ScanAndSyncLocalFilesAsync(CancellationToken cancellationToken);
}
