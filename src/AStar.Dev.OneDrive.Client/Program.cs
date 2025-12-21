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

    internal static IConfiguration Configuration = null!;

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
            App.Services = host.Services;
            ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
            LocalLogger = host.Services.GetRequiredService<ILogger<Program>>();
            ApplicationStarted(LocalLogger, applicationName);

            _ = BuildAvaloniaApp()
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
                Configuration = ctx.Configuration;
                HostExtensions.ConfigureApplicationServices(ctx, services);
            });

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
}
