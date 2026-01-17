using AStar.Dev.OneDrive.Client.Common;
using AStar.Dev.OneDrive.Client.Theme;
using Avalonia.Controls;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.ViewModels;

public partial class MainWindow : Window, IWindowPositionable
{
    private readonly IThemeSelectionHandler _themeHandler;
    private readonly IAutoSaveService _autoSaveService;
    private readonly MainWindowViewModel _viewModel;
    private readonly IMainWindowCoordinator _coordinator;

    public MainWindow(IMainWindowCoordinator coordinator, IThemeSelectionHandler themeHandler, IAutoSaveService autoSaveService, MainWindowViewModel vm)
    {
        InitializeComponent();
        Title = $"{vm.ApplicationName} - V{ApplicationMetadata.ApplicationVersion}";

        _coordinator = coordinator;
        _themeHandler = themeHandler;
        _autoSaveService = autoSaveService;
        _viewModel = vm;
        _coordinator.Initialize(this, vm);
        WireUpEventHandlers();
        InitializeThemeSelector();
        DataContext = vm;
    }

    private void WireUpEventHandlers()
    {
        Closing += OnClosing;
        _autoSaveService.MonitorForChanges(_viewModel, () => _coordinator.PersistUserPreferences(this, _viewModel));
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        CleanupEventHandlers();
        _autoSaveService.StopMonitoring(_viewModel);
        _coordinator.PersistUserPreferences(this, _viewModel);
    }

    private void CleanupEventHandlers() => Closing -= OnClosing;

    private void InitializeThemeSelector()
    {
        ComboBox? themeSelector = this.FindControl<ComboBox>("ThemeSelector");

        if(themeSelector is not null)
            _themeHandler.Initialize(themeSelector, _viewModel.UserPreferences);
    }
}
