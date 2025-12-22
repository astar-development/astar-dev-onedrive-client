using System.IO.Abstractions;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Client.Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSyncServices(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddSingleton<IFileSystem, FileSystem>();
        _ = services.AddSingleton<FileServices>();
        EntraIdSettings entraId = configuration.GetSection(EntraIdSettings.SectionName).Get<EntraIdSettings>()!;
        ApplicationSettings appSettings = configuration.GetSection(ApplicationSettings.SectionName).Get<ApplicationSettings>()!;
        _ = services.AddSingleton(entraId);
        _ = services.AddSingleton(appSettings);

        using(IServiceScope scope = services.BuildServiceProvider().CreateScope())
        {
            FileServices fileSystem = scope.ServiceProvider.GetRequiredService<FileServices>();
            var userPreferencesContent = fileSystem.GetFileContents(appSettings.FullUserPreferencesPath);
            UserPreferences userPreferences = userPreferencesContent.FromJson<UserPreferences>();
            _ = services.AddSingleton(userPreferences.UiSettings.SyncSettings);
            _ = services.AddSingleton(userPreferences);
        }

        _ = services.AddSingleton<TransferService>();
        _ = services.AddSingleton<SyncEngine>();

        return services;
    }
}
