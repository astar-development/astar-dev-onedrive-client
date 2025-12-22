using AStar.Dev.OneDrive.Client.Common;
using AStar.Dev.OneDrive.Client.Infrastructure.DependencyInjection;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.Services.DependencyInjection;
using AStar.Dev.OneDrive.Client.SettingsAndPreferences;
using AStar.Dev.OneDrive.Client.Theme;
using AStar.Dev.OneDrive.Client.ViewModels;
using AStar.Dev.OneDrive.Client.Views;
using AStar.Dev.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AStar.Dev.OneDrive.Client;

internal static class HostExtensions
{
    internal static void ConfigureApplicationServices(HostBuilderContext ctx, IServiceCollection services)
    {
        _ = services.AddLogging();
        IConfiguration config = ctx.Configuration;
        var connectionString = string.Empty;
        var localRoot = string.Empty;
        var msalClientId = string.Empty;
        ApplicationSettings appSettings = File.ReadAllText("appsettings.json").FromJson<ApplicationSettings>();

        using(IServiceScope scope = services.BuildServiceProvider().CreateScope())
        {

            try
            {
                // Ensure directories exist
                _ = Directory.CreateDirectory(appSettings.FullUserSyncPath);
                _ = Directory.CreateDirectory(appSettings.FullDatabaseDirectory);
                _ = Directory.CreateDirectory(appSettings.FullUserPreferencesDirectory);
                if(!File.Exists(appSettings.FullUserPreferencesPath))
                {
                    File.WriteAllText(appSettings.FullUserPreferencesPath, new UserPreferences().ToJson());
                }
            }
            catch(Exception ex)
            {
                throw new InvalidOperationException("Failed to create necessary application directories.", ex);
            }

            // App services
            _ = services.AddSyncServices(config);
        }

        using(IServiceScope scope = services.BuildServiceProvider().CreateScope())
        {
            EntraIdSettings entraId = scope.ServiceProvider.GetRequiredService<EntraIdSettings>();
            connectionString = $"Data Source={appSettings.FullDatabasePath}";
            localRoot = appSettings.FullUserSyncPath;
            msalClientId = entraId.ClientId;
        }

        _ = services.AddInfrastructure(connectionString, localRoot, msalClientId);

        // UI services and viewmodels
        _ = services.AddSingleton<MainWindow>();
        _ = services.AddSingleton<MainWindowViewModel>();
        _ = services.AddSingleton<SettingsViewModel>();
        _ = services.AddSingleton<DashboardViewModel>();
        _ = services.AddSingleton<IAutoSaveService, AutoSaveService>();
        _ = services.AddSingleton<ISettingsAndPreferencesService, SettingsAndPreferencesService>();
        _ = services.AddSingleton<IThemeMapper, ThemeMapper>();
        _ = services.AddSingleton<IThemeSelectionHandler, ThemeSelectionHandler>();
        _ = services.AddSingleton<IWindowPositionValidator, WindowPositionValidator>();
        _ = services.AddSingleton<IMainWindowCoordinator, MainWindowCoordinator>();
        _ = services.AddSingleton<ThemeService>();

        // Sync settings
        ServiceProvider servicesProvider = services.BuildServiceProvider();
        Action<IServiceProvider> initializer = servicesProvider.GetRequiredService<Action<IServiceProvider>>();
        initializer(servicesProvider);
    }
}
