using System.Diagnostics.CodeAnalysis;
using AStar.Dev.OneDrive.Client.Data;
using Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReactiveUI.Avalonia;
using Serilog;
using static AStar.Dev.Logging.Extensions.Messages.AStarLog.Application;
using static AStar.Dev.Logging.Extensions.Serilog.SerilogExtensions;

namespace AStar.Dev.OneDrive.Client;

[ExcludeFromCodeCoverage]
internal class Program
{
    protected Program() { }

    private static ILogger<Program> LocalLogger = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        ApplicationName applicationName = SetApplicationName();
        Log.Logger = CreateMinimalLogger();

        try
        {
            using IHost host = CreateHostBuilder(args).Build();
            host.Start();
            App.Services = host.Services;
            LocalLogger = host.Services.GetRequiredService<ILogger<Program>>();
            ApplicationStarted(LocalLogger, applicationName);

            _ = BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
        }
        catch(Exception ex)
        {
            ApplicationFailedToStart(LocalLogger, applicationName, ex.GetBaseException().Message);
        }
        finally
        {
            ApplicationStopped(LocalLogger, applicationName);
            Log.CloseAndFlush();
        }
    }

    private static ApplicationName SetApplicationName() => new(ApplicationMetadata.ApplicationName);

    private static IHostBuilder CreateHostBuilder(string[] args)
        => Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((ctx, cfg) =>
            {
                _ = cfg.SetBasePath(AppContext.BaseDirectory);
                _ = cfg.AddJsonFile("appsettings.json", false, false);
                _ = cfg.AddUserSecrets<App>(true);
            })
        .ConfigureServices(HostExtensions.ConfigureApplicationServices)
        .UseSerilog((context, services, configuration) =>
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
        );

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
}
