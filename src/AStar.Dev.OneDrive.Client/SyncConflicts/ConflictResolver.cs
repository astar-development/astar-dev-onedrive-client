using System.Globalization;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.FromV3;
using AStar.Dev.OneDrive.Client.FromV3.OneDriveServices;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;

#pragma warning disable CA1848 // Use LoggerMessage delegates - Using IsEnabled checks for performance

namespace AStar.Dev.OneDrive.Client.SyncConflicts;

/// <summary>
/// Implements conflict resolution strategies for sync conflicts.
/// </summary>
public sealed class ConflictResolver : IConflictResolver
{
    private readonly IGraphApiClient _graphApiClient;
    private readonly IFileMetadataRepository _metadataRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly ISyncConflictRepository _conflictRepo;
    private readonly ILocalFileScanner _localFileScanner;
    private readonly ILogger<ConflictResolver> _logger;

    public ConflictResolver(
        IGraphApiClient graphApiClient,
        IFileMetadataRepository metadataRepo,
        IAccountRepository accountRepo,
        ISyncConflictRepository conflictRepo,
        ILocalFileScanner localFileScanner,
        ILogger<ConflictResolver> logger)
    {
        _graphApiClient = graphApiClient ?? throw new ArgumentNullException(nameof(graphApiClient));
        _metadataRepo = metadataRepo ?? throw new ArgumentNullException(nameof(metadataRepo));
        _accountRepo = accountRepo ?? throw new ArgumentNullException(nameof(accountRepo));
        _conflictRepo = conflictRepo ?? throw new ArgumentNullException(nameof(conflictRepo));
        _localFileScanner = localFileScanner ?? throw new ArgumentNullException(nameof(localFileScanner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task ResolveAsync(
        Core.Entities.SyncConflict conflict,
        ConflictResolutionStrategy strategy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conflict);

        if(_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Resolving conflict for file {FilePath} in account {AccountId} with strategy {Strategy}",
                conflict.FilePath,
                conflict.AccountId,
                strategy);
        }

        AccountInfo account = await _accountRepo.GetByIdAsync(conflict.AccountId, cancellationToken) ?? throw new InvalidOperationException($"Account not found: {conflict.AccountId}");

        // Trim leading slash from OneDrive path for Windows compatibility
        var relativePath = conflict.FilePath.TrimStart('/');
        var localPath = Path.Combine(account.LocalSyncPath, relativePath);

        switch(strategy)
        {
            case ConflictResolutionStrategy.KeepLocal:
                await KeepLocalVersionAsync(account, conflict, localPath, cancellationToken);
                break;

            case ConflictResolutionStrategy.KeepRemote:
                await KeepRemoteVersionAsync(account, conflict, localPath, cancellationToken);
                break;

            case ConflictResolutionStrategy.KeepBoth:
                await KeepBothVersionsAsync(account, conflict, localPath, cancellationToken);
                break;

            case ConflictResolutionStrategy.None:
                if(_logger.IsEnabled(LogLevel.Information)) _logger.LogInformation("Skipping conflict resolution for {FilePath}", conflict.FilePath);

                return;
            case ConflictResolutionStrategy.KeepNewer:
                break;
            default:
                throw new ArgumentException($"Invalid resolution strategy: {strategy}", nameof(strategy));
        }

        // Mark conflict as resolved in database
     Core.Entities.SyncConflict resolvedConflict = conflict with
        {
            ResolutionStrategy = strategy,
            IsResolved = true
        };
        await _conflictRepo.UpdateAsync(resolvedConflict, cancellationToken);

        if(_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Successfully resolved conflict for {FilePath} with strategy {Strategy}",
                conflict.FilePath,
                strategy);
        }
    }

    private async Task KeepLocalVersionAsync(
        AccountInfo account,
        Core.Entities.SyncConflict conflict,
        string localPath,
        CancellationToken cancellationToken)
    {
        if(_logger.IsEnabled(LogLevel.Information)) _logger.LogInformation("Keeping local version of {FilePath}", conflict.FilePath);

        if(!File.Exists(localPath)) throw new FileNotFoundException($"Local file not found: {localPath}");

        // Get file metadata to retrieve OneDrive file ID
        FileMetadata metadata = await _metadataRepo.GetByPathAsync(
            account.AccountId,
            conflict.FilePath,
            cancellationToken) ?? throw new InvalidOperationException($"File metadata not found for {conflict.FilePath}");

        // Upload local file to OneDrive (overwrite remote)
        _ = await _graphApiClient.UploadFileAsync(
            account.AccountId,
            localPath,
            conflict.FilePath,
            progress: null,
            cancellationToken);

        // Update metadata
        var fileInfo = new FileInfo(localPath);
        FileMetadata updatedMetadata = metadata with
        {
            Size = fileInfo.Length,
            LastModifiedUtc = fileInfo.LastWriteTimeUtc,
            SyncStatus = FileSyncStatus.Synced,
            LastSyncDirection = SyncDirection.Upload
        };
        await _metadataRepo.UpdateAsync(updatedMetadata, cancellationToken);
    }

    private async Task KeepRemoteVersionAsync(
        AccountInfo account,
        Core.Entities.SyncConflict conflict,
        string localPath,
        CancellationToken cancellationToken)
    {
        if(_logger.IsEnabled(LogLevel.Information)) _logger.LogInformation("Keeping remote version of {FilePath}", conflict.FilePath);

        // Get file metadata to retrieve OneDrive file ID
        FileMetadata metadata = await _metadataRepo.GetByPathAsync(
            account.AccountId,
            conflict.FilePath,
            cancellationToken) ?? throw new InvalidOperationException($"File metadata not found for {conflict.FilePath}");

        var fileId = metadata.Id;

        // Download remote file from OneDrive (overwrite local)
        var localDirectory = Path.GetDirectoryName(localPath);
        if(!string.IsNullOrEmpty(localDirectory)) _ = Directory.CreateDirectory(localDirectory);

        await _graphApiClient.DownloadFileAsync(
            account.AccountId,
            fileId,
            localPath,
            cancellationToken);

        // Get remote metadata to get accurate timestamp
        DriveItem? remoteItem = await _graphApiClient.GetDriveItemAsync(account.AccountId, fileId, cancellationToken);
        if(remoteItem?.LastModifiedDateTime.HasValue == true)
        {
            // Set local file timestamp to match OneDrive's timestamp
            File.SetLastWriteTimeUtc(localPath, remoteItem.LastModifiedDateTime.Value.UtcDateTime);
        }

        // Compute hash of downloaded file
        var downloadedHash = await _localFileScanner.ComputeFileHashAsync(localPath, cancellationToken);

        // Update metadata
        var fileInfo = new FileInfo(localPath);
        FileMetadata updatedMetadata = metadata with
        {
            Size = fileInfo.Length,
            LastModifiedUtc = fileInfo.LastWriteTimeUtc,
            LocalHash = downloadedHash,
            SyncStatus = FileSyncStatus.Synced,
            LastSyncDirection = SyncDirection.Download
        };
        await _metadataRepo.UpdateAsync(updatedMetadata, cancellationToken);
    }

    private async Task KeepBothVersionsAsync(
        AccountInfo account,
        Core.Entities.SyncConflict conflict,
        string localPath,
        CancellationToken cancellationToken)
    {
        if(_logger.IsEnabled(LogLevel.Information)) _logger.LogInformation("Keeping both versions of {FilePath}", conflict.FilePath);

        if(!File.Exists(localPath)) throw new FileNotFoundException($"Local file not found: {localPath}");

        // Get file metadata to retrieve OneDrive file ID
        FileMetadata metadata = await _metadataRepo.GetByPathAsync(
            account.AccountId,
            conflict.FilePath,
            cancellationToken) ?? throw new InvalidOperationException($"File metadata not found for {conflict.FilePath}");

        var fileId = metadata.Id;

        // Rename local file with conflict suffix
        var directory = Path.GetDirectoryName(localPath) ?? string.Empty;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(localPath);
        var extension = Path.GetExtension(localPath);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture);
        var conflictFileName = $"{fileNameWithoutExtension} (Conflict {timestamp}){extension}";
        var conflictPath = Path.Combine(directory, conflictFileName);

        File.Move(localPath, conflictPath);

        if(_logger.IsEnabled(LogLevel.Information)) _logger.LogInformation("Renamed local file to {ConflictPath}", conflictPath);

        // Download remote version to original path
        var localDirectory = Path.GetDirectoryName(localPath);
        if(!string.IsNullOrEmpty(localDirectory)) _ = Directory.CreateDirectory(localDirectory);

        await _graphApiClient.DownloadFileAsync(
            account.AccountId,
            fileId,
            localPath,
            cancellationToken);

        // Get fresh metadata from OneDrive to get accurate remote timestamp
        DriveItem remoteItem = await _graphApiClient.GetDriveItemAsync(account.AccountId, fileId, cancellationToken) ?? throw new InvalidOperationException($"Failed to retrieve metadata for remote file {fileId}");

        // Set local file timestamp to match OneDrive's timestamp
        if(remoteItem.LastModifiedDateTime.HasValue) File.SetLastWriteTimeUtc(localPath, remoteItem.LastModifiedDateTime.Value.UtcDateTime);

        // Compute hash of downloaded file
        var downloadedHash = await _localFileScanner.ComputeFileHashAsync(localPath, cancellationToken);

        // Update metadata for downloaded remote version
        var fileInfo = new FileInfo(localPath);
        FileMetadata updatedMetadata = metadata with
        {
            Size = fileInfo.Length,
            LastModifiedUtc = fileInfo.LastWriteTimeUtc,
            CTag = remoteItem.CTag,
            ETag = remoteItem.ETag,
            LocalHash = downloadedHash,
            SyncStatus = FileSyncStatus.Synced,
            LastSyncDirection = SyncDirection.Download
        };
        await _metadataRepo.UpdateAsync(updatedMetadata, cancellationToken);

        if(_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Successfully kept both versions: {LocalPath} (remote) and {ConflictPath} (local)",
                localPath,
                conflictPath);
        }
    }
}
