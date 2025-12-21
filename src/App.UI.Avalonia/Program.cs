using System.Reflection;
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using static AStar.Dev.Logging.Extensions.Serilog.SerilogExtensions;
using static AStar.Dev.Logging.Extensions.AStarLog.Application;
using Microsoft.Extensions.Configuration;
using App.Infrastructure.Data;

namespace App.UI.Avalonia;

class Program
{
    public static async Task Main(string[] args)
    {
        var applicationName = "App.UI.Avalonia"; // Default name in case of failure before assembly load
        Log.Logger = CreateMinimalLogger();
        Microsoft.Extensions.Logging.ILogger<Program> logger = null!;
        try
        {
            applicationName = Assembly.GetExecutingAssembly().GetName().Name!;
            using IHost host = CreateHostBuilder(args).Build();
            await host.StartAsync();
            logger = host.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();

            logger.LogInformation("Starting {ApplicationName}...", applicationName);
            // ApplicationConfigurationStartUp configurationStartUp = host.Services.GetRequiredService<ApplicationConfigurationStartUp>();
            // configurationStartUp.Initialize();

            ApplicationStarted(logger, applicationName);
            _ = BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
        }
        catch(Exception ex)
        {
            ApplicationFailedToStart(logger, applicationName, ex.GetBaseException().Message);
            throw;
        }
        finally
        {
            ApplicationStopped(logger, applicationName);
            await Log.CloseAndFlushAsync();
        }

        _ = BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
        => Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((ctx, cfg) =>
            {
                _ = cfg.AddJsonFile("appsettings.json", true, false);
                _ = cfg.AddUserSecrets<App>(true);
            })
            .UseSerilog((ctx, services, loggerConfig) => _ = loggerConfig.ConfigureAStarDevelopmentLoggingDefaults(ctx.Configuration))
            .ConfigureServices((_, services) =>
            {
                
            });

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
}
