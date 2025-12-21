using System.Reflection;
using AStar.Dev.OneDrive.Client.Core.Utilities;
using AStar.Dev.OneDrive.Client.Infrastructure.DependencyInjection;
using AStar.Dev.OneDrive.Client.Services.DependencyInjection;
using AStar.Dev.OneDrive.Client.ViewModels;
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using static AStar.Dev.Logging.Extensions.Messages.AStarLog.Application;
using static AStar.Dev.Logging.Extensions.Serilog.SerilogExtensions;

namespace AStar.Dev.OneDrive.Client;

class Program
{
    protected Program() { }

    private static ILogger<Program> LocalLogger = null!;

    private static IConfiguration config = null!;

    public static async Task Main(string[] args)
    {
        var applicationName = "AStar.Dev.OneDrive.Client"; // Default name in case of failure before assembly load
        Log.Logger = CreateMinimalLogger();

        try
        {
            applicationName = Assembly.GetExecutingAssembly().GetName().Name!;
            using IHost host = CreateHostBuilder(args).Build();
            Log.Information("Starting: {ApplicationName}", applicationName);
            await host.StartAsync();
            //ApplicationStarted(LocalLogger, applicationName);

            App app = host.Services.GetRequiredService<App>();

            _ = BuildAvaloniaApp(app)
                    .StartWithClassicDesktopLifetime(args);
        }
        catch(Exception ex)
        {
            ApplicationFailedToStart(LocalLogger, applicationName, ex.GetBaseException().Message);
            throw;
        }
        finally
        {
            ApplicationStopped(LocalLogger, applicationName);
            await Log.CloseAndFlushAsync();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
        => Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((ctx, cfg) =>
            {
                _ = cfg.SetBasePath(AppContext.BaseDirectory);
                _ = cfg.AddJsonFile("appsettings.json", false, false);
                _ = cfg.AddUserSecrets<App>(true);
            })
            .ConfigureServices((ctx, services) =>
            {
                config = ctx.Configuration;
                // Infrastructure registration
                var dbPath = "Data Source=/home/jason/.config/astar-dev/astar-dev-onedrive-client/database/app.db"; // FIX THIS
                var localRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDriveSync"); // AND THIS
                var msalClientId = "3057f494-687d-4abb-a653-4b8066230b6e"; // CONFIG
                _ = services.AddInfrastructure(dbPath, localRoot, msalClientId);

                // App services
                _ = services.AddSyncServices(ctx.Configuration);

                // UI services and viewmodels
                _ = services.AddSingleton<App>();
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
            });

    private static AppBuilder BuildAvaloniaApp(App app)
        => AppBuilder.Configure(()=> app)
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
}
