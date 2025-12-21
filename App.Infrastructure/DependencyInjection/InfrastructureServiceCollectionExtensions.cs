using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using App.Core.Interfaces;
using App.Infrastructure.Data;
using App.Infrastructure.Repositories;
using App.Infrastructure.Graph;
using App.Infrastructure.Auth;
using App.Infrastructure.Filesystem;

namespace App.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string sqliteConnectionString, string localRoot, string msalClientId)
    {
        services.AddDbContext<AppDbContext>(opts => opts.UseSqlite(sqliteConnectionString));
        services.AddScoped<ISyncRepository, EfSyncRepository>();

        services.AddSingleton<IAuthService>(_ => new MsalAuthService(msalClientId));
        services.AddHttpClient<IGraphClient, GraphClientWrapper>()
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = true });

        services.AddSingleton<IFileSystemAdapter>(_ => new LocalFileSystemAdapter(localRoot));

        // Optional: register DbInitializer action
        services.AddSingleton<Action<IServiceProvider>>(sp =>
        {
            return provider =>
            {
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DbInitializer.EnsureDatabaseCreatedAndConfigured(db);
            };
        });

        return services;
    }
}
