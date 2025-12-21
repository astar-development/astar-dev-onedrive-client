using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
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
        _ = services.AddDbContext<AppDbContext>(opts => opts.UseSqlite(sqliteConnectionString));
        _ = services.AddScoped<ISyncRepository, EfSyncRepository>();

        _ = services.AddSingleton<IAuthService>(_ => new MsalAuthService(msalClientId));
        _ = services.AddHttpClient<IGraphClient, GraphClientWrapper>()
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = true });

        _ = services.AddSingleton<IFileSystemAdapter>(_ => new LocalFileSystemAdapter(localRoot));

        // Optional: register DbInitializer action
        _ = services.AddSingleton<Action<IServiceProvider>>(sp =>
        {
            return provider =>
            {
                using IServiceScope scope = provider.CreateScope();
                AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DbInitializer.EnsureDatabaseCreatedAndConfigured(db);
            };
        });

        return services;
    }
}
