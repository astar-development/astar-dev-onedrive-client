using System.IO.Abstractions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.Logging.Extensions.Messages;
using AStar.Dev.OneDrive.Client.Common;
using AStar.Dev.OneDrive.Client.Core.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.FromV3;
using AStar.Dev.OneDrive.Client.FromV3.Authentication;
using AStar.Dev.OneDrive.Client.FromV3.OneDriveServices;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.DependencyInjection;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.Services.DependencyInjection;
using AStar.Dev.OneDrive.Client.SettingsAndPreferences;
using AStar.Dev.OneDrive.Client.SyncConflicts;
using AStar.Dev.OneDrive.Client.Theme;
using AStar.Dev.OneDrive.Client.ViewModels;
using AStar.Dev.Source.Generators.OptionsBindingGeneration;
using AStar.Dev.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testably.Abstractions;

namespace AStar.Dev.OneDrive.Client;

internal static class HostExtensions
{
    internal static void ConfigureApplicationServices(HostBuilderContext context, IServiceCollection services)
    {
        _ = services.AddLogging();
        IConfiguration config = context.Configuration;

        _ = services.AddAutoRegisteredOptions(config);

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

        var authConfig = AuthConfiguration.LoadFromConfiguration(config);

        // Authentication - registered as singleton with factory
        _ = services.AddSingleton<IAuthService>(provider =>
            // AuthService.CreateAsync must be called synchronously during startup
            // This is acceptable as it's a one-time initialization cost
            AuthService.CreateAsync(authConfig).GetAwaiter().GetResult());

        // Repositories
        _ = services.AddScoped<IAccountRepository, AccountRepository>();
        _ = services.AddScoped<ISyncConfigurationRepository, SyncConfigurationRepository>();
        _ = services.AddScoped<IFileMetadataRepository, FileMetadataRepository>();
        _ = services.AddScoped<ISyncConflictRepository, SyncConflictRepository>();
        _ = services.AddScoped<ISyncSessionLogRepository, SyncSessionLogRepository>();
        _ = services.AddScoped<IFileOperationLogRepository, FileOperationLogRepository>();
        _ = services.AddScoped<IDebugLogRepository, DebugLogRepository>();
        // Services
        _ = services.AddSingleton<IFileSystem, RealFileSystem>();
        _ = services.AddSingleton<IFileWatcherService, FileWatcherService>();
        _ = services.AddSingleton<IAutoSyncCoordinator, AutoSyncCoordinator>();
        _ = services.AddSingleton<IAutoSyncSchedulerService, AutoSyncSchedulerService>();
        _ = services.AddScoped<IWindowPreferencesService, WindowPreferencesService>();
        _ = services.AddScoped<IGraphApiClient, GraphApiClient>();
        _ = services.AddScoped<IFolderTreeService, FolderTreeService>();
        _ = services.AddScoped<ISyncSelectionService, SyncSelectionService>();
        _ = services.AddScoped<ILocalFileScanner, LocalFileScanner>();
        _ = services.AddScoped<IRemoteChangeDetector, RemoteChangeDetector>();
        _ = services.AddScoped<IConflictResolver, ConflictResolver>();
        _ = services.AddScoped<ISyncEngine, SyncEngine>();
        _ = services.AddScoped<IDebugLogger, DebugLogger>();

        // ViewModels
        _ = services.AddTransient<AccountManagementViewModel>();
        _ = services.AddTransient<SyncTreeViewModel>();
        _ = services.AddTransient<ConflictResolutionViewModel>();
        _ = services.AddTransient<SyncProgressViewModel>();
        _ = services.AddTransient<UpdateAccountDetailsViewModel>();

        ServiceProvider servicesProvider = services.BuildServiceProvider();
        Action<IServiceProvider> initializer = servicesProvider.GetRequiredService<Action<IServiceProvider>>();
        initializer(servicesProvider);
    }

    private static void CreateDirectoriesAndUserPreferencesIfRequired(ApplicationSettings appSettings, ILogger<Program> log)
        => _ = Try.Run(() =>
            {
                _ = Directory.CreateDirectory(appSettings.FullUserSyncPath);
                _ = Directory.CreateDirectory(ApplicationSettings.FullDatabaseDirectory);
                _ = Directory.CreateDirectory(ApplicationSettings.FullUserPreferencesDirectory);
                if(!File.Exists(appSettings.FullUserPreferencesPath)) File.WriteAllText(appSettings.FullUserPreferencesPath, new UserPreferences().ToJson());
            })
            .TapError(ex => AStarLog.Application.ApplicationFailedToStart(log, ApplicationMetadata.ApplicationName, ex.Message));
}
