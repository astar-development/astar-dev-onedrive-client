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

namespace AStar.Dev.OneDrive.Client;

internal static class HostExtensions
{
    internal static void ConfigureApplicationServices(HostBuilderContext ctx, IServiceCollection services)
    {
        _ = services.AddLogging();
        IConfiguration config = ctx.Configuration;

        // Validate required configuration before proceeding
        ValidateConfiguration(config);

        var connectionString = string.Empty;
        var localRoot = string.Empty;
        var msalClientId = string.Empty;
        var currentDirectory = AppContext.BaseDirectory;
        ApplicationSettings appSettings = File.ReadAllText(Path.Combine(currentDirectory, "appsettings.json")).FromJson<ApplicationSettings>();

        using(IServiceScope scope = services.BuildServiceProvider().CreateScope())
        {

            try
            {
                // Ensure directories exist
                _ = Directory.CreateDirectory(appSettings.FullUserSyncPath);
                _ = Directory.CreateDirectory(ApplicationSettings.FullDatabaseDirectory);
                _ = Directory.CreateDirectory(ApplicationSettings.FullUserPreferencesDirectory);
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

        var msalConfigurationSettings = new MsalConfigurationSettings(
            msalClientId,
            appSettings.RedirectUri,
            appSettings.GraphUri,
            ctx.Configuration.GetSection("EntraId:Scopes").Get<string[]>() ?? [],
            appSettings.CachePrefix);

        _ = services.AddSingleton(msalConfigurationSettings);
        _ = services.AddInfrastructure(connectionString, localRoot, msalConfigurationSettings);

        // UI services and viewmodels
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

        // Sync settings
        ServiceProvider servicesProvider = services.BuildServiceProvider();
        Action<IServiceProvider> initializer = servicesProvider.GetRequiredService<Action<IServiceProvider>>();
        initializer(servicesProvider);
    }

    /// <summary>
    /// Validates that all required configuration values are present.
    /// Fails fast with clear error messages if configuration is missing.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <exception cref="InvalidOperationException">Thrown when required configuration is missing.</exception>
    private static void ValidateConfiguration(IConfiguration configuration)
    {
        var errors = new List<string>();

        // Validate Entra ID Client ID
        var clientId = configuration["EntraId:ClientId"];
        if(string.IsNullOrWhiteSpace(clientId))
        {
            errors.Add("EntraId:ClientId is not configured. " +
                      "Run: dotnet user-secrets set \"EntraId:ClientId\" \"YOUR-CLIENT-ID\"");
        }

        // Validate Entra ID Scopes
        var scopes = configuration.GetSection("EntraId:Scopes").Get<string[]>();
        if(scopes == null || scopes.Length == 0)
        {
            errors.Add("EntraId:Scopes are not configured. Required scopes: User.Read, Files.ReadWrite.All, offline_access");
        }

        // Validate Application Settings
        var appVersion = configuration["AStarDevOneDriveClient:ApplicationVersion"];
        if(string.IsNullOrWhiteSpace(appVersion))
        {
            errors.Add("AStarDevOneDriveClient:ApplicationVersion is not configured.");
        }

        if(errors.Count > 0)
        {
            var errorMessage = "Configuration validation failed:\n" + string.Join("\n", errors);
            throw new InvalidOperationException(errorMessage);
        }
    }
}
