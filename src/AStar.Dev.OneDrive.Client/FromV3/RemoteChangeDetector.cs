using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.FromV3.OneDriveServices;
using Microsoft.Graph.Models;

namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Service for detecting changes on OneDrive using delta queries.
/// </summary>
/// <remarks>
/// Note: Full delta query support requires Microsoft.Graph SDK capabilities.
/// This implementation provides a foundation that can be extended when delta APIs are integrated.
/// For now, it performs full scans and compares against known state.
/// </remarks>
public sealed class RemoteChangeDetector : IRemoteChangeDetector
{
    private readonly IGraphApiClient _graphApiClient;

    public RemoteChangeDetector(IGraphApiClient graphApiClient)
    {
        ArgumentNullException.ThrowIfNull(graphApiClient);
        _graphApiClient = graphApiClient;
    }

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<FileMetadata> Changes, string? NewDeltaLink)> DetectChangesAsync(
        string accountId,
        string folderPath,
        string? previousDeltaLink,
        CancellationToken cancellationToken = default)
    {
        await DebugLog.EntryAsync("RemoteChangeDetector.DetectChangesAsync", cancellationToken);
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(folderPath);

        cancellationToken.ThrowIfCancellationRequested();

        await DebugLog.InfoAsync("RemoteChangeDetector.DetectChangesAsync", $"Scanning folder: '{folderPath}'", cancellationToken);

        // Clean the folder path of Graph API prefixes before using it for file paths
        var cleanedFolderPath = CleanGraphApiPathPrefix(folderPath);
        if(cleanedFolderPath != folderPath) await DebugLog.InfoAsync("RemoteChangeDetector.DetectChangesAsync", $"Cleaned folder path from '{folderPath}' to '{cleanedFolderPath}'", cancellationToken);

        // For initial implementation, scan the folder tree
        // Note: For large OneDrive accounts (100k+ files), this can take several minutes
        // In future sprints, this will be enhanced with proper delta query support
        var changes = new List<FileMetadata>();
        DriveItem? rootItem = await GetFolderItemAsync(accountId, folderPath, cancellationToken);

        if(rootItem is not null)
        {
            await DebugLog.InfoAsync("RemoteChangeDetector.DetectChangesAsync", $"Folder item found, starting recursive scan", cancellationToken);
            // Add a practical limit for initial scan to prevent timeout
            // This will be removed when we implement proper delta query support
            const int maxFiles = 10000; // Limit initial scan to 10k files
            // Use cleaned path for building file paths
            await ScanFolderRecursiveAsync(accountId, rootItem, cleanedFolderPath, changes, cancellationToken, maxFiles);
            System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] Scan complete: {changes.Count} files found in {cleanedFolderPath}");
            await DebugLog.InfoAsync("RemoteChangeDetector.DetectChangesAsync", $"Scan complete: {changes.Count} files found", cancellationToken);
        }
        else
        {
            var errorMessage = $"Remote folder not found: '{folderPath}'. Please verify the folder exists and you have access to it.";
            await DebugLog.ErrorAsync("RemoteChangeDetector.DetectChangesAsync", errorMessage, null, cancellationToken);
            throw new InvalidOperationException(errorMessage);
        }

        // Generate a simple delta token based on timestamp
        // In production, this would be the actual deltaLink from Graph API
        var newDeltaLink = $"delta_{DateTime.UtcNow:yyyyMMddHHmmss}";

        await DebugLog.ExitAsync("RemoteChangeDetector.DetectChangesAsync", cancellationToken);

        return (changes, newDeltaLink);
    }

    private async Task<DriveItem?> GetFolderItemAsync(string accountId, string folderPath, CancellationToken cancellationToken)
    {
        await DebugLog.EntryAsync("RemoteChangeDetector.GetFolderItemAsync", cancellationToken);
        await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Looking for folder: '{folderPath}'", cancellationToken);

        // Defensive: Strip Graph API path prefixes that might be in stored paths
        // This handles cases where paths like "/drive/root:/Documents" or "/drives/{id}/root:/Documents" were stored
        if(folderPath.StartsWith("/drive/root:", StringComparison.OrdinalIgnoreCase))
        {
            folderPath = folderPath["/drive/root:".Length..];
            await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Stripped '/drive/root:' prefix, new path: '{folderPath}'", cancellationToken);
        }
        else if(folderPath.StartsWith("/drives/", StringComparison.OrdinalIgnoreCase))
        {
            var rootIndex = folderPath.IndexOf("/root:", StringComparison.OrdinalIgnoreCase);
            if(rootIndex >= 0)
            {
                folderPath = folderPath[(rootIndex + "/root:".Length)..];
                await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Stripped '/drives/.../root:' prefix, new path: '{folderPath}'", cancellationToken);
            }
        }

        // For root or empty path, return the drive root
        if(folderPath == "/" || string.IsNullOrEmpty(folderPath))
        {
            await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", "Returning drive root", cancellationToken);
            return await _graphApiClient.GetDriveRootAsync(accountId, cancellationToken);
        }

        // Clean up the path
        folderPath = folderPath.Trim('/');
        await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Trimmed path: '{folderPath}'", cancellationToken);

        // Get root and traverse path segments
        DriveItem? currentItem = await _graphApiClient.GetDriveRootAsync(accountId, cancellationToken);
        if(currentItem?.Id is null)
        {
            await DebugLog.ErrorAsync("RemoteChangeDetector.GetFolderItemAsync", "Failed to get drive root", null, cancellationToken);
            return null;
        }

        var pathSegments = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Path segments: [{string.Join(", ", pathSegments)}]", cancellationToken);

        foreach(var segment in pathSegments)
        {
            await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Looking for segment: '{segment}'", cancellationToken);
            IEnumerable<DriveItem> children = await _graphApiClient.GetDriveItemChildrenAsync(accountId, currentItem.Id, cancellationToken);
            await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Found {children.Count()} children in current folder", cancellationToken);

            currentItem = children.FirstOrDefault(c =>
                c.Name?.Equals(segment, StringComparison.OrdinalIgnoreCase) == true &&
                c.Folder is not null);

            if(currentItem?.Id is null)
            {
                await DebugLog.ErrorAsync("RemoteChangeDetector.GetFolderItemAsync", $"Folder segment '{segment}' not found", null, cancellationToken);
                return null;
            }

            await DebugLog.InfoAsync("RemoteChangeDetector.GetFolderItemAsync", $"Found folder: '{currentItem.Name}' (ID: {currentItem.Id})", cancellationToken);
        }

        await DebugLog.ExitAsync("RemoteChangeDetector.GetFolderItemAsync", cancellationToken);

        return currentItem;
    }

    private async Task ScanFolderRecursiveAsync(
        string accountId,
        DriveItem parentItem,
        string currentPath,
        List<FileMetadata> changes,
        CancellationToken cancellationToken,
        int maxFiles = int.MaxValue)
    {
        await DebugLog.EntryAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", cancellationToken);
        await DebugLog.InfoAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", $"Scanning folder: '{currentPath}' (ID: {parentItem.Id})", cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if(changes.Count >= maxFiles)
        {
            await DebugLog.InfoAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", $"Max files limit ({maxFiles}) reached", cancellationToken);
            return; // Reached the limit
        }

        if(parentItem.Id is null)
        {
            await DebugLog.ErrorAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", "Parent item ID is null", null, cancellationToken);
            return;
        }

        IEnumerable<DriveItem> children = await _graphApiClient.GetDriveItemChildrenAsync(accountId, parentItem.Id, cancellationToken);
        await DebugLog.InfoAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", $"Found {children.Count()} items in '{currentPath}'", cancellationToken);

        var fileCount = 0;
        var folderCount = 0;

        foreach(DriveItem item in children)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if(changes.Count >= maxFiles) return; // Reached the limit

            if(item.File is not null && item.Id is not null && item.Name is not null)
            {
                // It's a file
                fileCount++;
                var itemPath = CombinePaths(currentPath, item.Name);
                FileMetadata metadata = ConvertToFileMetadata(accountId, item, itemPath);
                changes.Add(metadata);
                if(changes.Count % 500 == 0) System.Diagnostics.Debug.WriteLine($"[RemoteChangeDetector] Progress: {changes.Count} files scanned");
            }
            else if(item.Folder is not null && item.Id is not null && item.Name is not null)
            {
                // It's a folder - scan recursively
                folderCount++;
                var itemPath = CombinePaths(currentPath, item.Name);
                await DebugLog.InfoAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", $"Recursing into subfolder: '{item.Name}'", cancellationToken);
                await ScanFolderRecursiveAsync(accountId, item, itemPath, changes, cancellationToken, maxFiles);
            }
        }

        await DebugLog.InfoAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", $"Completed '{currentPath}': {fileCount} files, {folderCount} subfolders", cancellationToken);

        await DebugLog.ExitAsync("RemoteChangeDetector.ScanFolderRecursiveAsync", cancellationToken);
    }

    private static FileMetadata ConvertToFileMetadata(string accountId, DriveItem item, string path) => new(
            Id: item.Id ?? string.Empty,
            AccountId: accountId,
            Name: item.Name ?? string.Empty,
            Path: path,
            Size: item.Size ?? 0,
            LastModifiedUtc: item.LastModifiedDateTime?.UtcDateTime ?? DateTime.UtcNow,
            LocalPath: string.Empty, // Will be set during download
            CTag: item.CTag,
            ETag: item.ETag,
            LocalHash: null, // Will be computed after download
            SyncStatus: FileSyncStatus.PendingDownload,
            LastSyncDirection: SyncDirection.Download);

    private static string CombinePaths(string basePath, string name)
    {
        basePath = basePath.Replace('\\', '/');
        name = name.Replace('\\', '/');

        if(!basePath.EndsWith('/')) basePath += '/';

        if(name.StartsWith('/')) name = name[1..];

        return basePath + name;
    }

    /// <summary>
    /// Cleans Graph API path prefixes from folder paths.
    /// </summary>
    /// <param name="path">The path that may contain Graph API prefixes.</param>
    /// <returns>The cleaned path without Graph API prefixes.</returns>
    private static string CleanGraphApiPathPrefix(string path)
    {
        if(string.IsNullOrEmpty(path)) return path;

        // Strip /drive/root: prefix
        if(path.StartsWith("/drive/root:", StringComparison.OrdinalIgnoreCase)) return path["/drive/root:".Length..];

        // Strip /drives/{drive-id}/root: prefix
        if(path.StartsWith("/drives/", StringComparison.OrdinalIgnoreCase))
        {
            var rootIndex = path.IndexOf("/root:", StringComparison.OrdinalIgnoreCase);
            if(rootIndex >= 0) return path[(rootIndex + "/root:".Length)..];
        }

        return path;
    }
}
