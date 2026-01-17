using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Repositories;

namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Service for managing folder selection state in the sync tree.
/// </summary>
public sealed partial class SyncSelectionService : ISyncSelectionService
{
    private readonly ISyncConfigurationRepository? _configurationRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncSelectionService"/> class.
    /// </summary>
    public SyncSelectionService()
    {
        // Parameterless constructor for backward compatibility with existing tests
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncSelectionService"/> class with database persistence.
    /// </summary>
    /// <param name="configurationRepository">The sync configuration repository.</param>
    public SyncSelectionService(ISyncConfigurationRepository configurationRepository) => _configurationRepository = configurationRepository;
    /// <inheritdoc/>
    public void SetSelection(OneDriveFolderNode folder, bool isSelected)
    {
        ArgumentNullException.ThrowIfNull(folder);

        SelectionState selectionState = isSelected ? SelectionState.Checked : SelectionState.Unchecked;
        folder.SelectionState = selectionState;
        folder.IsSelected = isSelected;

        // Cascade to all children
        CascadeSelectionToChildren(folder, selectionState);
    }

    /// <inheritdoc/>
    public void UpdateParentStates(OneDriveFolderNode folder, List<OneDriveFolderNode> rootFolders)
    {
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentNullException.ThrowIfNull(rootFolders);

        if(folder.ParentId is null)
        {
            return;
        }

        OneDriveFolderNode? parent = FindNodeById(rootFolders, folder.ParentId);
        if(parent is null)
        {
            return;
        }

        SelectionState calculatedState = CalculateStateFromChildren(parent);
        parent.SelectionState = calculatedState;
        parent.IsSelected = calculatedState switch
        {
            SelectionState.Checked => true,
            SelectionState.Unchecked => false,
            SelectionState.Indeterminate => null,
            _ => null
        };

        // Continue propagating upward
        UpdateParentStates(parent, rootFolders);
    }

    /// <inheritdoc/>
    public List<OneDriveFolderNode> GetSelectedFolders(List<OneDriveFolderNode> rootFolders)
    {
        ArgumentNullException.ThrowIfNull(rootFolders);

        var selectedFolders = new List<OneDriveFolderNode>();
        CollectSelectedFolders(rootFolders, selectedFolders);
        return selectedFolders;
    }

    /// <inheritdoc/>
    public void ClearAllSelections(List<OneDriveFolderNode> rootFolders)
    {
        ArgumentNullException.ThrowIfNull(rootFolders);

        foreach(OneDriveFolderNode folder in rootFolders)
        {
            SetSelection(folder, false);
        }
    }

    /// <inheritdoc/>
    public SelectionState CalculateStateFromChildren(OneDriveFolderNode folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        if(folder.Children.Count == 0)
        {
            return folder.SelectionState;
        }

        var checkedCount = 0;
        var uncheckedCount = 0;
        var indeterminateCount = 0;

        foreach(OneDriveFolderNode child in folder.Children)
        {
            switch(child.SelectionState)
            {
                case SelectionState.Checked:
                    checkedCount++;
                    break;
                case SelectionState.Unchecked:
                    uncheckedCount++;
                    break;
                case SelectionState.Indeterminate:
                    indeterminateCount++;
                    break;
                default:
                    break;
            }
        }

        // If any child is indeterminate, parent is indeterminate
        if(indeterminateCount > 0)
        {
            return SelectionState.Indeterminate;
        }

        // If all children are checked, parent is checked
        if(checkedCount == folder.Children.Count)
        {
            return SelectionState.Checked;
        }

        // If all children are unchecked, parent is unchecked
        if(uncheckedCount == folder.Children.Count && folder.IsSelected == false)
        {
            return SelectionState.Unchecked;
        }

        // Mixed state = indeterminate
        return SelectionState.Indeterminate;
    }

    private static void CascadeSelectionToChildren(OneDriveFolderNode folder, SelectionState state)
    {
        foreach(OneDriveFolderNode child in folder.Children)
        {
            child.SelectionState = state;
            child.IsSelected = state switch
            {
                SelectionState.Checked => true,
                SelectionState.Unchecked => false,
                SelectionState.Indeterminate => throw new NotImplementedException(),
                _ => null
            };

            CascadeSelectionToChildren(child, state);
        }
    }

    private static void CollectSelectedFolders(List<OneDriveFolderNode> folders, List<OneDriveFolderNode> result)
    {
        foreach(OneDriveFolderNode folder in folders)
        {
            if(folder.SelectionState == SelectionState.Checked &&
                !string.IsNullOrEmpty(folder.Path) &&
                !string.IsNullOrEmpty(folder.Name))
            {
                result.Add(folder);
            }

            CollectSelectedFolders([.. folder.Children], result);
        }
    }

    private static OneDriveFolderNode? FindNodeById(List<OneDriveFolderNode> folders, string nodeId)
    {
        foreach(OneDriveFolderNode folder in folders)
        {
            if(folder.Id == nodeId)
            {
                return folder;
            }

            OneDriveFolderNode? foundInChildren = FindNodeById([.. folder.Children], nodeId);
            if(foundInChildren is not null)
            {
                return foundInChildren;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task SaveSelectionsToDatabaseAsync(string accountId, List<OneDriveFolderNode> rootFolders, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(rootFolders);

        if(_configurationRepository is null)
        {
            return; // No persistence configured
        }

        // Get all checked folders
        List<OneDriveFolderNode> selectedFolders = GetSelectedFolders(rootFolders);

        // Convert to SyncConfiguration records
        IEnumerable<SyncConfiguration> configurations = selectedFolders.Select(folder => new SyncConfiguration(
            Id: 0, // Will be auto-generated
            AccountId: accountId,
            FolderPath: folder.Path,
            IsSelected: true,
            LastModifiedUtc: DateTime.UtcNow
        ));

        // Save batch (replaces all existing selections for this account)
        await _configurationRepository.SaveBatchAsync(accountId, configurations, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task LoadSelectionsFromDatabaseAsync(string accountId, List<OneDriveFolderNode> rootFolders, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(rootFolders);

        // Set account context for debug logging
        DebugLogContext.SetAccountId(accountId);

        if(_configurationRepository is null)
        {
            return; // No persistence configured
        }

        // Get saved folder paths
        IReadOnlyList<string> savedFolderPaths = await _configurationRepository.GetSelectedFoldersAsync(accountId, cancellationToken);

        await DebugLog.InfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", $"Loading selections for account {accountId}", cancellationToken);
        await DebugLog.InfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", $"Found {savedFolderPaths.Count} saved paths in database", cancellationToken);
        foreach(var path in savedFolderPaths)
        {
            await DebugLog.InfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", $"DB Path: {path}", cancellationToken);
        }

        // Normalize paths by removing Graph API prefixes for comparison
        var normalizedSavedPaths = savedFolderPaths
            .Select(NormalizePathForComparison)
            .ToList();

        await DebugLog.InfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", "Normalized paths:", cancellationToken);
        foreach(var path in normalizedSavedPaths)
        {
            await DebugLog.InfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", $"Normalized: {path}", cancellationToken);
        }

        // Build lookup dictionary for fast path-to-node resolution, using normalized paths
        var pathToNodeMap = new Dictionary<string, OneDriveFolderNode>(StringComparer.OrdinalIgnoreCase);
        BuildPathLookup(rootFolders, pathToNodeMap);

        // Build a normalized path-to-node map for robust matching
        var normalizedPathToNodeMap = new Dictionary<string, OneDriveFolderNode>(StringComparer.OrdinalIgnoreCase);
        foreach(KeyValuePair<string, OneDriveFolderNode> kvp in pathToNodeMap)
        {
            var normalized = NormalizePathForComparison(kvp.Key);
            if(!normalizedPathToNodeMap.ContainsKey(normalized))
            {
                normalizedPathToNodeMap[normalized] = kvp.Value;
            }
        }

        // Initialize ALL folders to Unchecked first
        foreach(OneDriveFolderNode folder in pathToNodeMap.Values)
        {
            folder.SelectionState = SelectionState.Unchecked;
            folder.IsSelected = false;
        }

        // Then set saved selections to Checked using normalized matching
        if(savedFolderPaths.Count > 0)
        {
            for(var i = 0; i < savedFolderPaths.Count; i++)
            {
                var normalizedPath = normalizedSavedPaths[i];
                if(normalizedPathToNodeMap.TryGetValue(normalizedPath, out OneDriveFolderNode? folder))
                {
                    SetSelection(folder, true);
                }
                // Silently ignore folders that no longer exist (deleted or renamed)
            }

            // For root folders that aren't loaded: check if they have selected descendants
            // This handles the case where only a deep subfolder is selected
            await DebugLog.InfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", "Checking root folders for selected descendants", cancellationToken);
            foreach(OneDriveFolderNode rootFolder in rootFolders)
            {
                await DebugLog.InfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", $"Checking root: {rootFolder.Path} (State: {rootFolder.SelectionState})", cancellationToken);

                // Skip if root is already explicitly selected
                if(rootFolder.SelectionState == SelectionState.Checked)
                {
                    await DebugLog.InfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", "Already checked, skipping", cancellationToken);
                    continue;
                }

                // Normalize root path for comparison
                var normalizedRootPath = NormalizePathForComparison(rootFolder.Path);
                await DebugLog.InfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", $"Root normalized to: {normalizedRootPath}", cancellationToken);

                // Check if any saved path is a descendant of this root
                var hasSelectedDescendants = normalizedSavedPaths.Any(path =>
                    path.StartsWith(normalizedRootPath + "/", StringComparison.OrdinalIgnoreCase) ||
                    (normalizedRootPath == "/" && path.StartsWith("/", StringComparison.OrdinalIgnoreCase) && path != "/"));

                await DebugLog.InfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", $"Has selected descendants: {hasSelectedDescendants}", cancellationToken);

                if(hasSelectedDescendants)
                {
                    // Set root to indeterminate since it has selected descendants
                    await DebugLog.InfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", $"Setting {rootFolder.Path} to Indeterminate", cancellationToken);
                    rootFolder.SelectionState = SelectionState.Indeterminate;
                    rootFolder.IsSelected = null;
                    await DebugLog.InfoAsync("SyncSelectionService.LoadSelectionsFromDatabaseAsync", $"After setting - State: {rootFolder.SelectionState}, IsSelected: {rootFolder.IsSelected}", cancellationToken);
                }
            }
        }

        // NOTE: We don't call RecalculateParentStates here because:
        // 1. Root folders don't have parents
        // 2. Their children aren't loaded yet (just placeholder nodes)
        // 3. We've already manually set indeterminate state for roots with selected descendants
    }

    private static void BuildPathLookup(List<OneDriveFolderNode> folders, Dictionary<string, OneDriveFolderNode> pathMap)
    {
        foreach(OneDriveFolderNode folder in folders)
        {
            if(string.IsNullOrEmpty(folder.Path))
            {
                continue;
            }

            pathMap[folder.Path] = folder;

            if(folder.Children.Count > 0)
            {
                BuildPathLookup([.. folder.Children], pathMap);
            }
        }
    }

    /// <inheritdoc/>
    public void UpdateParentState(OneDriveFolderNode folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        if(folder.Children.Count == 0)
        {
            return; // No children, nothing to calculate
        }

        SelectionState calculatedState = CalculateStateFromChildren(folder);
        folder.SelectionState = calculatedState;
        folder.IsSelected = calculatedState switch
        {
            SelectionState.Checked => true,
            SelectionState.Unchecked => false,
            SelectionState.Indeterminate => null,
            _ => null
        };
    }

    /// <summary>
    /// Normalizes a path by removing Graph API prefixes for comparison.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path without Graph API prefixes.</returns>
    private static string NormalizePathForComparison(string path)
    {
        if(string.IsNullOrEmpty(path))
        {
            return path;
        }

        // Remove /drives/{id}/root: prefix
        var drivesPattern = @"^/drives/[^/]+/root:";
        if(System.Text.RegularExpressions.Regex.IsMatch(path, drivesPattern))
        {
            path = System.Text.RegularExpressions.Regex.Replace(path, drivesPattern, string.Empty);
        }

        // Remove /drive/root: prefix
        if(path.StartsWith("/drive/root:", StringComparison.OrdinalIgnoreCase))
        {
            path = path["/drive/root:".Length..];
        }

        // Ensure path starts with /
        if(!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        return path;
    }

    private void RecalculateParentStates(OneDriveFolderNode folder)
    {
        if(folder.Children.Count > 0)
        {
            // Recursively update children first
            foreach(OneDriveFolderNode child in folder.Children)
            {
                RecalculateParentStates(child);
            }

            // Then update this folder's state based on children
            SelectionState calculatedState = CalculateStateFromChildren(folder);
            folder.SelectionState = calculatedState;
            folder.IsSelected = calculatedState switch
            {
                SelectionState.Checked => true,
                SelectionState.Unchecked => false,
                SelectionState.Indeterminate => null,
                _ => null
            };
        }
    }
}
