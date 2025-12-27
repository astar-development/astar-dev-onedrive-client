namespace AStar.Dev.OneDrive.Client.ViewModels;

/// <summary>
/// Exposes UI update methods for sync status, progress, and notifications.
/// </summary>
public interface ISyncStatusTarget
{
    /// <summary>Sets the status message for the sync operation.</summary>
    void SetStatus(string status);

    /// <summary>Sets the progress percentage for the sync operation.</summary>
    void SetProgress(double percent);

    /// <summary>Adds a message to the recent transfers list.</summary>
    void AddRecentTransfer(string message);

    /// <summary>Called when a sync operation completes successfully.</summary>
    void OnSyncCompleted();

    /// <summary>Called when a sync operation fails.</summary>
    void OnSyncFailed(string operation, Exception ex);

    /// <summary>Called when a sync operation is cancelled.</summary>
    void OnSyncCancelled(string operation);

    /// <summary>Sets the signed-in state for the user.</summary>
    void SetSignedIn(bool value);
    void SetFullSync(bool value);
    void SetIncrementalSync(bool value);
}
