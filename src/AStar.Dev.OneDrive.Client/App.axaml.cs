using Avalonia;
using Avalonia.Markup.Xaml;

namespace AStar.Dev.OneDrive.Client;

public partial class App : Application
{
   // private IServiceProvider? _services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    // public override void OnFrameworkInitializationCompleted()
    // {
    //     var services = new ServiceCollection();
    //     _ = services.AddLogging(cfg => cfg.AddConsole());

    //     // Infrastructure registration
    //     var dbPath = "Data Source=/home/jason/.config/astar-dev/astar-dev-onedrive-client/database/app.db"; // FIX THIS
    //     var localRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDriveSync"); // AND THIS
    //     var msalClientId = "3057f494-687d-4abb-a653-4b8066230b6e"; // CONFIG
    //     _ = services.AddInfrastructure(dbPath, localRoot, msalClientId);

    //     // App services
    //     _ = services.AddSyncServices();

    //     // UI services and viewmodels
    //     _ = services.AddSingleton<MainWindow>();
    //     _ = services.AddSingleton<MainWindowViewModel>();
    //     _ = services.AddSingleton<SettingsViewModel>();
    //     _ = services.AddSingleton<DashboardViewModel>();

    //     // Sync settings
    //     _ = services.AddSingleton(new SyncSettings(ParallelDownloads: 4, BatchSize: 50));

    //     _services = services.BuildServiceProvider();
    //     // Ensure DB created and configured
    //     Action<IServiceProvider> initializer = _services.GetRequiredService<Action<IServiceProvider>>();
    //     initializer(_services);

    //     if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    //     {
    //         MainWindow main = _services.GetRequiredService<MainWindow>();
    //         desktop.MainWindow = main;
    //     }

    //     base.OnFrameworkInitializationCompleted();
    // }
}
