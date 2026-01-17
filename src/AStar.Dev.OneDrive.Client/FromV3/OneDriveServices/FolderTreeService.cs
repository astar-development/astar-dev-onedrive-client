using AStar.Dev.OneDrive.Client.FromV3.Authentication;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Repositories;
using Microsoft.Graph.Models;

namespace AStar.Dev.OneDrive.Client.FromV3.OneDriveServices;

/// <summary>
/// Service for retrieving and managing OneDrive folder hierarchies.
/// </summary>
public sealed class FolderTreeService : IFolderTreeService
{
    private readonly IGraphApiClient _graphApiClient;
    private readonly IAuthService _authService;
    private readonly ISyncConfigurationRepository _syncConfigurationRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="FolderTreeService"/> class.
    /// </summary>
    /// <param name="graphApiClient">The Graph API client.</param>
    /// <param name="authService">The authentication service.</param>
    /// <param name="syncConfigurationRepository">The sync configuration repository.</param>
    public FolderTreeService(IGraphApiClient graphApiClient, IAuthService authService, ISyncConfigurationRepository syncConfigurationRepository)
    {
        ArgumentNullException.ThrowIfNull(graphApiClient);
        ArgumentNullException.ThrowIfNull(authService);
        _graphApiClient = graphApiClient;
        _authService = authService;
        _syncConfigurationRepository = syncConfigurationRepository;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OneDriveFolderNode>> GetRootFoldersAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        // Verify account is authenticated
        var isAuthenticated = await _authService.IsAuthenticatedAsync(accountId, cancellationToken);
        if(!isAuthenticated)
        {
            return [];
        }

        IEnumerable<DriveItem> driveItems = await _graphApiClient.GetRootChildrenAsync(accountId, cancellationToken);
        IEnumerable<DriveItem> folders = driveItems.Where(item => item.Folder is not null);

        var nodes = new List<OneDriveFolderNode>();
        foreach(DriveItem? item in folders)
        {
            if(item.Id is null || item.Name is null)
            {
                continue;
            }

            var node = new OneDriveFolderNode(
                id: item.Id,
                name: item.Name,
                path: $"/{item.Name}",
                parentId: item.ParentReference?.Id,
                isFolder: true)
            {
                IsSelected = false
            };

            // Add placeholder child so expansion toggle appears
            node.Children.Add(new OneDriveFolderNode());

            nodes.Add(node);
            _syncConfigurationRepository.AddAsync(new SyncConfiguration
            (
                Id: 0,
                AccountId: accountId,
                FolderPath: node.Path,
                IsSelected: false,
                LastModifiedUtc: DateTime.UtcNow
            ), cancellationToken).Wait(cancellationToken);
        }

        return nodes;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OneDriveFolderNode>> GetChildFoldersAsync(string accountId, string parentFolderId, bool? parentIsSelected = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(parentFolderId);

        // Verify account is authenticated
        var isAuthenticated = await _authService.IsAuthenticatedAsync(accountId, cancellationToken);
        if(!isAuthenticated)
        {
            return [];
        }

        // Get parent folder to build paths
        DriveItem? parentItem = await _graphApiClient.GetDriveItemAsync(accountId, parentFolderId, cancellationToken);
        var parentPath = parentItem?.ParentReference?.Path is not null
            ? $"{parentItem.ParentReference.Path}/{parentItem.Name}"
            : $"/{parentItem?.Name}";

        IEnumerable<DriveItem> driveItems = await _graphApiClient.GetDriveItemChildrenAsync(accountId, parentFolderId, cancellationToken);
        IEnumerable<DriveItem> folders = driveItems.Where(item => item.Folder is not null);

        var nodes = new List<OneDriveFolderNode>();
        foreach(DriveItem? item in folders)
        {
            if(item.Id is null || item.Name is null)
            {
                continue;
            }

            // Retrieve or create sync config for this folder
            SyncConfiguration updatedSyncConfiguration = await _syncConfigurationRepository.AddAsync(new SyncConfiguration
            (
                Id: 0,
                AccountId: accountId,
                FolderPath: $"{parentPath}/{item.Name}",
                IsSelected: false,
                LastModifiedUtc: DateTime.UtcNow
            ), cancellationToken);

            // If parent is selected, propagate selection to children
            bool? isSelected = parentIsSelected == true || updatedSyncConfiguration.IsSelected;

            var node = new OneDriveFolderNode(
                id: item.Id,
                name: item.Name,
                path: $"{parentPath}/{item.Name}",
                parentId: parentFolderId,
                isFolder: true)
            {
                IsSelected = isSelected
            };

            // Add placeholder child so expansion toggle appears
            node.Children.Add(new OneDriveFolderNode());

            nodes.Add(node);
        }

        return nodes;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OneDriveFolderNode>> GetFolderHierarchyAsync(string accountId, int? maxDepth = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        // Verify account is authenticated
        var isAuthenticated = await _authService.IsAuthenticatedAsync(accountId, cancellationToken);
        if(!isAuthenticated)
        {
            return [];
        }

        IReadOnlyList<OneDriveFolderNode> rootFolders = await GetRootFoldersAsync(accountId, cancellationToken);
        var rootList = rootFolders.ToList();

        if(maxDepth is null or > 0)
        {
            foreach(OneDriveFolderNode? folder in rootList)
            {
                await LoadChildrenRecursiveAsync(accountId, folder, maxDepth, 1, cancellationToken);
            }
        }

        return rootList;
    }

    private async Task LoadChildrenRecursiveAsync(
        string accountId,
        OneDriveFolderNode parentNode,
        int? maxDepth,
        int currentDepth,
        CancellationToken cancellationToken)
    {
        if(maxDepth.HasValue && currentDepth >= maxDepth.Value)
        {
            return;
        }

        IReadOnlyList<OneDriveFolderNode> children = await GetChildFoldersAsync(accountId, parentNode.Id, parentNode.IsSelected, cancellationToken);
        foreach(OneDriveFolderNode child in children)
        {
            parentNode.Children.Add(child);
            await LoadChildrenRecursiveAsync(accountId, child, maxDepth, currentDepth + 1, cancellationToken);
        }

        parentNode.ChildrenLoaded = true;
    }
}
