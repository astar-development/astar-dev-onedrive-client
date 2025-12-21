using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using App.Infrastructure.DependencyInjection;
using App.Services.DependencyInjection;
using App.Core.Utilities;
using App.UI.Avalonia.ViewModels;

namespace App.UI.Avalonia;

public partial class App : Application
{
    private IServiceProvider? _services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddLogging(cfg => cfg.AddConsole());

        // Infrastructure registration
        var dbPath = "Data Source=/home/jason/.config/astar-dev/astar-dev-onedrive-client/database/app.db"; // FIX THIS
        var localRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OneDriveSync");
        var msalClientId = "3057f494-687d-4abb-a653-4b8066230b6e"; // CONFIG
        services.AddInfrastructure(dbPath, localRoot, msalClientId);

        // App services
        services.AddSyncServices();

        // UI services and viewmodels
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<DashboardViewModel>();

        // Sync settings
        services.AddSingleton(new SyncSettings(ParallelDownloads: 4, BatchSize: 50));

        _services = services.BuildServiceProvider();

        // Ensure DB created and configured
        var initializer = _services.GetRequiredService<Action<IServiceProvider>>();
        initializer(_services);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var main = _services.GetRequiredService<MainWindow>();
            desktop.MainWindow = main;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
