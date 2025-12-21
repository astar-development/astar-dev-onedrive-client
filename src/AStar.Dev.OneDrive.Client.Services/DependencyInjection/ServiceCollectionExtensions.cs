using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Client.Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSyncServices(this IServiceCollection services)
    {
        _ = services.AddSingleton(new SyncSettings(ParallelDownloads: 4, BatchSize: 50));
        _ = services.AddSingleton<TransferService>();
        _ = services.AddSingleton<SyncEngine>();

        return services;
    }
    public static IServiceCollection AddSyncServices(this IServiceCollection services, IConfiguration configuration)
    {
        EntraIdSettings entraId = configuration.GetSection(EntraIdSettings.SectionName).Get<EntraIdSettings>()!;
        ApplicationSettings appSettings = configuration.GetSection(ApplicationSettings.SectionName).Get<ApplicationSettings>()!;
        UserPreferences userPreferences = File.ReadAllText(appSettings.FullUserPreferencesPath).FromJson<UserPreferences>();
        _ = services.AddSingleton(new SyncSettings(ParallelDownloads: userPreferences.UiSettings.MaxParallelDownloads, BatchSize: userPreferences.UiSettings.DownloadBatchSize));
        _ = services.AddSingleton<TransferService>();
        _ = services.AddSingleton<SyncEngine>();
        _ = services.AddSingleton(entraId);
        _ = services.AddSingleton(appSettings);
        _ = services.AddSingleton(userPreferences);

        return services;
    }
}
