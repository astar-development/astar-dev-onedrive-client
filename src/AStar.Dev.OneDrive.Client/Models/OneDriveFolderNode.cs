using System.Collections.ObjectModel;
using AStar.Dev.OneDrive.Client.Core.Entities.Enums;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.Models;

/// <summary>
/// Represents a folder or file node in the OneDrive folder hierarchy.
/// </summary>
/// <remarks>
/// This model is used for building the folder tree UI and managing sync selection.
/// The Children collection supports lazy loading for efficient tree rendering.
/// </remarks>
public sealed class OneDriveFolderNode : ReactiveObject
{
    /// <summary>
    /// Gets or sets the unique identifier for this item (OneDrive DriveItem ID).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the folder or file.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full path from the OneDrive root.
    /// </summary>
    /// <remarks>
    /// Example: "/Documents/Work/Projects"
    /// </remarks>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent node's ID, or null if this is a root-level item.
    /// </summary>
    public string? ParentId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this node represents a folder (true) or file (false).
    /// </summary>
    public bool IsFolder { get; set; }

    /// <summary>
    /// Gets the collection of child nodes.
    /// </summary>
    /// <remarks>
    /// This collection supports lazy loading - children are populated when the node is expanded in the UI.
    /// </remarks>
    public ObservableCollection<OneDriveFolderNode> Children { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether child nodes have been loaded from the API.
    /// </summary>
    /// <remarks>
    /// Used to prevent redundant API calls when expanding/collapsing tree nodes.
    /// </remarks>
    public bool ChildrenLoaded { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this node is expanded in the tree view.
    /// </summary>
    public bool IsExpanded
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this node is currently loading its children.
    /// </summary>
    public bool IsLoading
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets or sets the selection state for sync operations.
    /// </summary>
    /// <remarks>
    /// Use Checked/Unchecked for explicit user selection.
    /// Indeterminate is calculated when some (but not all) children are selected.
    /// </remarks>
    public SelectionState SelectionState
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets or sets a nullable boolean representing the selection state.
    /// </summary>
    /// <remarks>
    /// true = Checked, false = Unchecked, null = Indeterminate.
    /// This property provides checkbox-friendly binding for tri-state selection.
    /// </remarks>
    public bool? IsSelected
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OneDriveFolderNode"/> class.
    /// </summary>
    public OneDriveFolderNode()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OneDriveFolderNode"/> class with specified properties.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The display name.</param>
    /// <param name="path">The full path.</param>
    /// <param name="parentId">The parent node ID.</param>
    /// <param name="isFolder">Whether this is a folder.</param>
    public OneDriveFolderNode(string id, string name, string path, string? parentId, bool isFolder)
    {
        Id = id;
        Name = name;
        Path = path;
        ParentId = parentId;
        IsFolder = isFolder;
    }
}
