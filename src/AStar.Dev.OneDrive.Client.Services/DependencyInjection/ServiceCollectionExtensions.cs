using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Client.Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSyncServices(this IServiceCollection services)
    {
        _ = services.AddSingleton<TransferService>();
        _ = services.AddSingleton<SyncEngine>();

        return services;
    }
}
