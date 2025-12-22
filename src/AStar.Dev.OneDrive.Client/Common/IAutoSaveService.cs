using AStar.Dev.OneDrive.Client.ViewModels;

namespace AStar.Dev.OneDrive.Client.Common;

/// <summary>
///     Defines the contract for automatically saving user preferences when specific properties change.
/// </summary>
public interface IAutoSaveService
{
    /// <summary>
    ///     Begins monitoring the view model for property changes and invokes the save action when appropriate.
    /// </summary>
    /// <param name="viewModel">The view model to monitor for property changes.</param>
    /// <param name="saveAction">The action to invoke when a monitored property changes.</param>
    void MonitorForChanges(MainWindowViewModel viewModel, Action saveAction);

    /// <summary>
    ///     Stops monitoring the view model for property changes and unsubscribes from events.
    /// </summary>
    /// <param name="viewModel">The view model to stop monitoring.</param>
    void StopMonitoring(MainWindowViewModel viewModel);
}
