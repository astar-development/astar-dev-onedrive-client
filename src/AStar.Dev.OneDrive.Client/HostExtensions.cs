using AStar.Dev.Functional.Extensions;
using AStar.Dev.Logging.Extensions.Messages;
using AStar.Dev.OneDrive.Client.Common;
using AStar.Dev.OneDrive.Client.Core.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.Infrastructure.DependencyInjection;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.Services.DependencyInjection;
using AStar.Dev.OneDrive.Client.SettingsAndPreferences;
using AStar.Dev.OneDrive.Client.Theme;
using AStar.Dev.OneDrive.Client.ViewModels;
using AStar.Dev.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AStar.Dev.OneDrive.Client;

internal static class HostExtensions
{
    internal static void ConfigureApplicationServices(HostBuilderContext context, IServiceCollection services)
    {
        _ = services.AddLogging();
        IConfiguration config = context.Configuration;

        RegisterConfiguration(context, services);
        //_ = services.AddAutoRegisteredOptions(config);

        var connectionString = string.Empty;
        var localRoot = string.Empty;
        var msalClientId = string.Empty;
        using(IServiceScope scope = services.BuildServiceProvider().CreateScope())
        {
            ILogger<Program> log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            ApplicationSettings appSettings = scope.ServiceProvider.GetRequiredService<IOptions<ApplicationSettings>>().Value;
            CreateDirectoriesAndUserPreferencesIfRequired(appSettings, log);

            _ = services.AddSyncServices(config);
            EntraIdSettings entraId = scope.ServiceProvider.GetRequiredService<IOptions<EntraIdSettings>>().Value;

            connectionString = $"Data Source={appSettings.FullDatabasePath}";
            localRoot = appSettings.FullUserSyncPath;
            msalClientId = entraId.ClientId;

            var msalConfigurationSettings = new MsalConfigurationSettings(
            msalClientId,
            appSettings.RedirectUri,
            appSettings.GraphUri,
            context.Configuration.GetSection("EntraId:Scopes").Get<string[]>() ?? [],
            appSettings.CachePrefix);

            _ = services.AddSingleton(msalConfigurationSettings);
            _ = services.AddInfrastructure(connectionString, localRoot, msalConfigurationSettings);
        }

        _ = services.AddSingleton<MainWindow>();
        _ = services.AddSingleton<MainWindowViewModel>();
        _ = services.AddSingleton<DashboardViewModel>();
        _ = services.AddSingleton<IAutoSaveService, AutoSaveService>();
        _ = services.AddSingleton<ISettingsAndPreferencesService, SettingsAndPreferencesService>();
        _ = services.AddSingleton<IThemeService, ThemeService>();
        _ = services.AddSingleton<IThemeMapper, ThemeMapper>();
        _ = services.AddSingleton<ISyncCommandService, SyncCommandService>();
        _ = services.AddSingleton<IThemeSelectionHandler, ThemeSelectionHandler>();
        _ = services.AddSingleton<IWindowPositionValidator, WindowPositionValidator>();
        _ = services.AddSingleton<IMainWindowCoordinator, MainWindowCoordinator>();
        _ = services.AddSingleton<ThemeService>();

        ServiceProvider servicesProvider = services.BuildServiceProvider();
        Action<IServiceProvider> initializer = servicesProvider.GetRequiredService<Action<IServiceProvider>>();
        initializer(servicesProvider);
    }

    private static void RegisterConfiguration(HostBuilderContext context, IServiceCollection services)
    {
        _ = services
                .AddOptions<ApplicationSettings>()
                .Bind(context.Configuration.GetSection(ApplicationSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        _ = services
                .AddOptions<EntraIdSettings>()
                .Bind(context.Configuration.GetSection(EntraIdSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
    }

    private static void CreateDirectoriesAndUserPreferencesIfRequired(ApplicationSettings appSettings, ILogger<Program> log)
        => _ = Try.Run(() =>
            {
                _ = Directory.CreateDirectory(appSettings.FullUserSyncPath);
                _ = Directory.CreateDirectory(ApplicationSettings.FullDatabaseDirectory);
                _ = Directory.CreateDirectory(ApplicationSettings.FullUserPreferencesDirectory);
                if(!File.Exists(appSettings.FullUserPreferencesPath))
                {
                    File.WriteAllText(appSettings.FullUserPreferencesPath, new UserPreferences().ToJson());
                }
            })
            .TapError(ex => AStarLog.Application.ApplicationFailedToStart(log, "AStar.Dev.OneDrive.Client", ex.Message));
}
