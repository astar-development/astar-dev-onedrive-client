using AStar.Dev.OneDrive.Client.Infrastructure.DependencyInjection;
using AStar.Dev.OneDrive.Client.Services.DependencyInjection;
using AStar.Dev.OneDrive.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AStar.Dev.OneDrive.Client;

internal static class HostExtensions
{
    internal static void ConfigureApplicationServices(HostBuilderContext ctx, IServiceCollection services)
    {
        var dbPath = "Data Source=/home/jason/.config/astar-dev/astar-dev-onedrive-client/database/app.db"; // FIX THIS
                var localRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDriveSync"); // AND THIS
                var msalClientId = "3057f494-687d-4abb-a653-4b8066230b6e"; // CONFIG
                _ = services.AddInfrastructure(dbPath, localRoot, msalClientId);

                // App services
                _ = services.AddSyncServices(ctx.Configuration);

                // UI services and viewmodels
                _ = services.AddSingleton<MainWindow>();
                _ = services.AddSingleton<MainWindowViewModel>();
                _ = services.AddSingleton<SettingsViewModel>();
                _ = services.AddSingleton<DashboardViewModel>();

                // Sync settings
                //_ = services.AddSingleton(new SyncSettings(ParallelDownloads: 4, BatchSize: 50));
                ServiceProvider servicesProvider = services.BuildServiceProvider();
                // LocalLogger = servicesProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
                Action<IServiceProvider> initializer = servicesProvider.GetRequiredService<Action<IServiceProvider>>();
                initializer(servicesProvider);
    }
}