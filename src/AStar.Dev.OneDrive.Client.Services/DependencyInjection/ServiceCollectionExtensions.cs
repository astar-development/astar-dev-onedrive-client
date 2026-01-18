
using System.IO.Abstractions;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.Services.Syncronisation;
using AStar.Dev.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            _ = services.AddSingleton(userPreferences);
        }

        _ = services.AddSingleton<TransferService>();
        _ = services.AddSingleton<SyncEngine>();
        _ = services.AddSingleton<ISyncEngine>(sp => sp.GetRequiredService<SyncEngine>());
        _ = services.AddSingleton<ITransferService>(sp => sp.GetRequiredService<TransferService>());
        _ = services.AddSingleton<IHealthCheckService, ApplicationHealthCheckService>();
        _ = services.AddSingleton<ISyncronisationCoordinator, SyncronisationCoordinator>();

        // Register supporting abstractions for DI
        _ = services.AddSingleton<SyncProgressReporter>();
        _ = services.AddSingleton<ISyncErrorLogger, SyncErrorLogger>(sp =>
            new SyncErrorLogger(sp.GetRequiredService<ILogger<TransferService>>()));
        _ = services.AddSingleton<IDeltaPageProcessor, DeltaPageProcessor>();
        _ = services.AddSingleton<ILocalFileScanner, LocalFileScanner>();
        _ = services.AddSingleton<IChannelFactory, ChannelFactory>();
        _ = services.AddTransient<IDownloadQueueProducer, DownloadQueueProducer>(sp =>
            new DownloadQueueProducer(
                sp.GetRequiredService<ISyncRepository>(),
                sp.GetRequiredService<UserPreferences>().UiSettings.SyncSettings.DownloadBatchSize > 0
                    ? sp.GetRequiredService<UserPreferences>().UiSettings.SyncSettings.DownloadBatchSize
                    : 100));
        _ = services.AddSingleton<IDownloadQueueConsumer, DownloadQueueConsumer>();

        // Upload queue DI
        _ = services.AddSingleton<IUploadQueueProducer, UploadQueueProducer>();
        _ = services.AddSingleton<IUploadQueueConsumer, UploadQueueConsumer>();

        // Update TransferService registration to inject upload queue dependencies
        _ = services.AddSingleton(sp =>
            new TransferService(
                sp.GetRequiredService<IFileSystemAdapter>(),
                sp.GetRequiredService<IGraphClient>(),
                sp.GetRequiredService<ISyncRepository>(),
                sp.GetRequiredService<ILogger<TransferService>>(),
                sp.GetRequiredService<UserPreferences>(),
                sp.GetRequiredService<SyncProgressReporter>(),
                sp.GetRequiredService<ISyncErrorLogger>(),
                sp.GetRequiredService<IChannelFactory>(),
                sp.GetRequiredService<IDownloadQueueProducer>(),
                sp.GetRequiredService<IDownloadQueueConsumer>(),
                sp.GetRequiredService<IUploadQueueProducer>(),
                sp.GetRequiredService<IUploadQueueConsumer>()
            ));
        _ = services.AddSingleton<ITransferService>(sp => sp.GetRequiredService<TransferService>());

        return services;
    }
}
