namespace AStar.Dev.OneDrive.Client.FromV3.Models;

/// <summary>
/// Represents the selection state of a folder in the sync tree.
/// </summary>
public enum SelectionState
{
    /// <summary>
    /// The folder and all its children are not selected for sync.
    /// </summary>
    Unchecked = 0,

    /// <summary>
    /// The folder and all its children are selected for sync.
    /// </summary>
    Checked = 1,

    /// <summary>
    /// Some (but not all) of the folder's children are selected for sync.
    /// </summary>
    /// <remarks>
    /// This state is calculated based on child selections and cannot be directly set by the user.
    /// </remarks>
    Indeterminate = 2
}
