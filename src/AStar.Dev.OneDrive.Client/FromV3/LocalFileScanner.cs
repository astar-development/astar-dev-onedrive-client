using System.IO.Abstractions;
using System.Security.Cryptography;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Models.Enums;

namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Service for scanning local file system and detecting file changes.
/// </summary>
public sealed class LocalFileScanner : ILocalFileScanner
{
    private readonly IFileSystem _fileSystem;

    public LocalFileScanner(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FileMetadata>> ScanFolderAsync(
        string accountId,
        string localFolderPath,
        string oneDriveFolderPath,
        CancellationToken cancellationToken = default)
    {
        var indexOfDrives = localFolderPath.IndexOf("drives", StringComparison.OrdinalIgnoreCase);
        if(indexOfDrives >= 0)
        {
            var indexOfColon = localFolderPath.IndexOf(":/", StringComparison.OrdinalIgnoreCase);
            if(indexOfColon > 0)
            {
                var part1 = localFolderPath[..indexOfDrives];
                var part2 = localFolderPath[(indexOfColon + 2)..];
                localFolderPath = part1 + part2;
            }
        }

        await DebugLog.EntryAsync("LocalFileScanner.ScanFolderAsync", cancellationToken);
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(localFolderPath);
        ArgumentNullException.ThrowIfNull(oneDriveFolderPath);

        if(!_fileSystem.Directory.Exists(localFolderPath))
        {
            return [];
        }

        await DebugLog.InfoAsync("LocalFileScanner.ScanFolderAsync", $"Scanning folder: {localFolderPath}", cancellationToken);
        var fileMetadataList = new List<FileMetadata>();
        await ScanDirectoryRecursiveAsync(
            accountId,
            localFolderPath,
            oneDriveFolderPath,
            fileMetadataList,
            cancellationToken);
        await DebugLog.ExitAsync("LocalFileScanner.ScanFolderAsync", cancellationToken);

        return fileMetadataList;
    }

    private async Task ScanDirectoryRecursiveAsync(
        string accountId,
        string currentLocalPath,
        string currentOneDrivePath,
        List<FileMetadata> fileMetadataList,
        CancellationToken cancellationToken)
    {
        await DebugLog.EntryAsync("LocalFileScanner.ScanDirectoryRecursiveAsync", cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var files = _fileSystem.Directory.GetFiles(currentLocalPath);
            foreach(var filePath in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    IFileInfo fileInfo = _fileSystem.FileInfo.New(filePath);
                    if(!fileInfo.Exists)
                    {
                        continue;
                    }

                    var relativePath = GetRelativePath(currentLocalPath, filePath);
                    var oneDrivePath = CombinePaths(currentOneDrivePath, relativePath);
                    var hash = await ComputeFileHashAsync(filePath, cancellationToken);

                    var metadata = new FileMetadata(
                        Id: string.Empty, // Will be populated from OneDrive after upload
                        AccountId: accountId,
                        Name: fileInfo.Name,
                        Path: oneDrivePath,
                        Size: fileInfo.Length,
                        LastModifiedUtc: fileInfo.LastWriteTimeUtc,
                        LocalPath: filePath,
                        CTag: null,
                        ETag: null,
                        LocalHash: hash,
                        SyncStatus: FileSyncStatus.PendingUpload,
                        LastSyncDirection: null);

                    fileMetadataList.Add(metadata);
                }
                catch(UnauthorizedAccessException)
                {
                    // Skip files we don't have access to
                    continue;
                }
                catch(IOException)
                {
                    // Skip files that are locked or in use
                    continue;
                }
            }

            var directories = _fileSystem.Directory.GetDirectories(currentLocalPath);
            foreach(var directory in directories)
            {
                var relativePath = GetRelativePath(currentLocalPath, directory);
                var oneDrivePath = CombinePaths(currentOneDrivePath, relativePath);

                await ScanDirectoryRecursiveAsync(
                    accountId,
                    directory,
                    oneDrivePath,
                    fileMetadataList,
                    cancellationToken);
            }
        }
        catch(UnauthorizedAccessException)
        {
            // Skip directories we don't have access to
        }
        catch(DirectoryNotFoundException)
        {
            // Directory was deleted during scan
        }

        await DebugLog.ExitAsync("LocalFileScanner.ScanDirectoryRecursiveAsync", cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        using FileSystemStream stream = _fileSystem.File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes);
    }

    private static string GetRelativePath(string basePath, string fullPath)
    {
        var baseUri = new Uri(EnsureTrailingSlash(basePath));
        var fullUri = new Uri(fullPath);
        Uri relativeUri = baseUri.MakeRelativeUri(fullUri);
        return Uri.UnescapeDataString(relativeUri.ToString());
    }

    private static string EnsureTrailingSlash(string path)
        => !path.EndsWith(Path.DirectorySeparatorChar) && !path.EndsWith(Path.AltDirectorySeparatorChar)
                ? path + Path.DirectorySeparatorChar
                : path;

    private static string CombinePaths(string basePath, string relativePath)
    {
        basePath = basePath.Replace('\\', '/');
        relativePath = relativePath.Replace('\\', '/');

        if(!basePath.EndsWith('/'))
        {
            basePath += '/';
        }

        if(relativePath.StartsWith('/'))
        {
            relativePath = relativePath[1..];
        }

        return basePath + relativePath;
    }
}
