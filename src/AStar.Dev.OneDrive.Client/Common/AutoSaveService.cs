using System.ComponentModel;
using AStar.Dev.OneDrive.Client.ViewModels;

namespace AStar.Dev.OneDrive.Client.Common;

/// <summary>
///     Provides automatic persistence of user preferences when specific view model properties change.
///     Monitors property change notifications and triggers save actions accordingly.
/// </summary>
public class AutoSaveService : IAutoSaveService
{
    private PropertyChangedEventHandler? _handler;

    /// <inheritdoc />
    public void MonitorForChanges(MainWindowViewModel viewModel, Action saveAction)
    {
        _handler = (_, e) =>
        {
            if(e.PropertyName == nameof(MainWindowViewModel.SyncStatusMessage))
                saveAction();
        };

        viewModel.PropertyChanged += _handler;
    }

    /// <inheritdoc />
    public void StopMonitoring(MainWindowViewModel viewModel)
    {
        if(_handler is not null)
            viewModel.PropertyChanged -= _handler;
    }
}
