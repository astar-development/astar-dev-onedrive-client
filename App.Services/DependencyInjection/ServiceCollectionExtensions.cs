using Microsoft.Extensions.DependencyInjection;
using App.Core.Interfaces;

namespace App.Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSyncServices(this IServiceCollection services)
    {
        services.AddSingleton<SyncSettings>(_ => new SyncSettings(ParallelDownloads: 4, BatchSize: 50));
        services.AddSingleton<TransferService>();
        services.AddSingleton<SyncEngine>();
        return services;
    }
}
