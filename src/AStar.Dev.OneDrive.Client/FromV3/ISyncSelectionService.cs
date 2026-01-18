using AStar.Dev.OneDrive.Client.Core.Entities.Enums;
using AStar.Dev.OneDrive.Client.Models;

namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Service for managing folder selection state in the sync tree.
/// </summary>
/// <remarks>
/// This service handles tri-state checkbox logic including:
/// - Cascading selection from parent to children
/// - Upward propagation to calculate indeterminate states
/// - Tracking selected folders for sync operations
/// </remarks>
public interface ISyncSelectionService
{
    /// <summary>
    /// Sets the selection state of a folder and cascades the change to all descendants.
    /// </summary>
    /// <param name="folder">The folder to update.</param>
    /// <param name="isSelected">True to select, false to deselect.</param>
    /// <remarks>
    /// When a folder is selected/deselected, all child folders inherit the same state.
    /// This method also triggers upward propagation to update parent states.
    /// </remarks>
    void SetSelection(OneDriveFolderNode folder, bool isSelected);

    /// <summary>
    /// Updates the selection state of parent folders based on their children's states.
    /// </summary>
    /// <param name="folder">The folder whose parents should be updated.</param>
    /// <param name="rootFolders">The root-level folders to search within.</param>
    /// <remarks>
    /// This method calculates indeterminate states when some (but not all) children are selected.
    /// It propagates changes up the tree to the root level.
    /// </remarks>
    void UpdateParentStates(OneDriveFolderNode folder, List<OneDriveFolderNode> rootFolders);

    /// <summary>
    /// Updates the selection state of a single folder based on its children's states.
    /// </summary>
    /// <param name="folder">The folder to update.</param>
    /// <remarks>
    /// This method calculates the folder's state (Checked/Unchecked/Indeterminate) based on
    /// the current state of its children. Use this after loading children from the database.
    /// </remarks>
    void UpdateParentState(OneDriveFolderNode folder);

    /// <summary>
    /// Gets all folders that are explicitly selected for sync (excludes indeterminate).
    /// </summary>
    /// <param name="rootFolders">The root-level folders to search within.</param>
    /// <returns>List of folders with SelectionState.Checked.</returns>
    /// <remarks>
    /// This method performs a recursive search to find all checked folders.
    /// Indeterminate folders are excluded as they represent partial selection.
    /// </remarks>
    List<OneDriveFolderNode> GetSelectedFolders(List<OneDriveFolderNode> rootFolders);

    /// <summary>
    /// Clears all selection states, setting all folders to Unchecked.
    /// </summary>
    /// <param name="rootFolders">The root-level folders to clear.</param>
    void ClearAllSelections(List<OneDriveFolderNode> rootFolders);

    /// <summary>
    /// Calculates the selection state of a folder based on its children.
    /// </summary>
    /// <param name="folder">The folder to evaluate.</param>
    /// <returns>
    /// Checked if all children are checked,
    /// Unchecked if all children are unchecked,
    /// Indeterminate if children have mixed states.
    /// </returns>
    /// <remarks>
    /// This is a helper method for determining parent states during upward propagation.
    /// </remarks>
    SelectionState CalculateStateFromChildren(OneDriveFolderNode folder);

    /// <summary>
    /// Saves the current selection state to the database for persistence.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="rootFolders">The root-level folders containing current selections.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Only explicitly checked folders are persisted. Indeterminate states are recalculated on load.
    /// </remarks>
    Task SaveSelectionsToDatabaseAsync(string accountId, List<OneDriveFolderNode> rootFolders, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads saved selection state from the database and applies it to the folder tree.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="rootFolders">The root-level folders to apply selections to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// After loading, parent states are automatically recalculated to reflect indeterminate states.
    /// </remarks>
    Task LoadSelectionsFromDatabaseAsync(string accountId, List<OneDriveFolderNode> rootFolders, CancellationToken cancellationToken = default);
}
