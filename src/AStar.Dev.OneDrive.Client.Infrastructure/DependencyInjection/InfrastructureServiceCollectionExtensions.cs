using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Infrastructure.Auth;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.FileSystem;
using AStar.Dev.OneDrive.Client.Infrastructure.Graph;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Client.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string sqliteConnectionString, string localRoot, string msalClientId)
    {
        _ = services.AddDbContext<AppDbContext>(opts => opts.UseSqlite(sqliteConnectionString));
        _ = services.AddScoped<ISyncRepository, EfSyncRepository>();

        _ = services.AddSingleton<IAuthService>(_ => new MsalAuthService(msalClientId));
        _ = services.AddHttpClient<IGraphClient, GraphClientWrapper>()
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = true });

        _ = services.AddSingleton<IFileSystemAdapter>(_ => new LocalFileSystemAdapter(localRoot));

        _ = services.AddSingleton<Action<IServiceProvider>>(sp => provider =>
            {
                using IServiceScope scope = provider.CreateScope();
                AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DbInitializer.EnsureDatabaseCreatedAndConfigured(db);
            });

        return services;
    }
}
