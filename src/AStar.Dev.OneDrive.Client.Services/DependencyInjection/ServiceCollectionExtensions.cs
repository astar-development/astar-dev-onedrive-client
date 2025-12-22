using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Client.Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSyncServices(this IServiceCollection services)
    {
        _ = services.AddSingleton(new SyncSettings{MaxParallelDownloads = 4, DownloadBatchSize= 50, MaxRetries = 3, RetryBaseDelayMs = 500});
        _ = services.AddSingleton<TransferService>();
        _ = services.AddSingleton<SyncEngine>();

        return services;
    }
    public static IServiceCollection AddSyncServices(this IServiceCollection services, IConfiguration configuration)
    {
        EntraIdSettings entraId = configuration.GetSection(EntraIdSettings.SectionName).Get<EntraIdSettings>()!;
        ApplicationSettings appSettings = configuration.GetSection(ApplicationSettings.SectionName).Get<ApplicationSettings>()!;
        UserPreferences userPreferences = File.ReadAllText(appSettings.FullUserPreferencesPath).FromJson<UserPreferences>();
        
        _ = services.AddSingleton(userPreferences.UiSettings.SyncSettings);
        _ = services.AddSingleton<TransferService>();
        _ = services.AddSingleton<SyncEngine>();
        _ = services.AddSingleton(entraId);
        _ = services.AddSingleton(appSettings);
        _ = services.AddSingleton(userPreferences);

        return services;
    }
}
