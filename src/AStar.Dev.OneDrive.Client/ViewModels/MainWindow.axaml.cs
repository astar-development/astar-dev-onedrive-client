using AStar.Dev.OneDrive.Client.SettingsAndPreferences;
using AStar.Dev.OneDrive.Client.Theme;
using Avalonia.Controls;

namespace AStar.Dev.OneDrive.Client.ViewModels;

public partial class MainWindow : Window
{
    private readonly IThemeSelectionHandler _themeHandler;
    private readonly UserPreferences _userPreferences;

    public MainWindow(IThemeSelectionHandler themeHandler, ISettingsAndPreferencesService settingsAndPreferencesService, MainWindowViewModel vm)
    {
        InitializeComponent();
        _themeHandler = themeHandler;
        _userPreferences =settingsAndPreferencesService.Load();
        InitializeThemeSelector();
        DataContext = vm;
    }

    private void InitializeThemeSelector()
    {
        ComboBox? themeSelector = this.FindControl<ComboBox>("ThemeSelector");

        if(themeSelector is not null)
            _themeHandler.Initialize(themeSelector, _userPreferences);
    }
}
