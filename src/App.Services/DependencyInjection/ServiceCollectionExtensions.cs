using Microsoft.Extensions.DependencyInjection;

namespace App.Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSyncServices(this IServiceCollection services)
    {
        _ = services.AddSingleton<SyncSettings>(_ => new SyncSettings(ParallelDownloads: 4, BatchSize: 50));
        _ = services.AddSingleton<TransferService>();
        _ = services.AddSingleton<SyncEngine>();
        return services;
    }
}
