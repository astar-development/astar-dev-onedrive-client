using System.IO.Abstractions;
using AStar.Dev.OneDrive.Client.Core.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Infrastructure.Auth;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.FileSystem;
using AStar.Dev.OneDrive.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Client.Infrastructure.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Retry;

namespace AStar.Dev.OneDrive.Client.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string sqliteConnectionString, string localRoot, MsalConfigurationSettings msalConfigurationSettings)
    {
        _ = services.AddDbContext<AppDbContext>(opts => opts.UseSqlite(sqliteConnectionString));
        _ = services.AddDbContextFactory<AppDbContext>(opts => opts.UseSqlite(sqliteConnectionString));
        _ = services.AddScoped<ISyncRepository>(sp =>
        {
            IDbContextFactory<AppDbContext> factory = sp.GetRequiredService<IDbContextFactory<AppDbContext>>();
            ILogger<EfSyncRepository> logger = sp.GetRequiredService<ILogger<EfSyncRepository>>();
            return new EfSyncRepository(factory, logger);
        });

        _ = services.AddSingleton<IAuthService>(_ => new MsalAuthService(msalConfigurationSettings));
        _ = services.AddHttpClient<IGraphClient, GraphClientWrapper>()
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    MaxConnectionsPerServer = 10
                })
                .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromMinutes(5))
                .AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(GetCircuitBreakerPolicy());

        _ = services.AddSingleton<IFileSystem>(sp => new System.IO.Abstractions.FileSystem());
        _ = services.AddSingleton<IFileSystemAdapter>(sp => new LocalFileSystemAdapter(localRoot, sp.GetRequiredService<IFileSystem>()));

        // Health checks
        _ = services.AddHealthChecks()
                        .AddCheck<DatabaseHealthCheck>("database")
                        .AddCheck<GraphApiHealthCheck>("graph_api");

        _ = services.AddSingleton<Action<IServiceProvider>>(sp => provider =>
            {
                using IServiceScope scope = provider.CreateScope();
                AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DbInitializer.EnsureDatabaseCreatedAndConfigured(db);
            });

        return services;
    }

    /// <summary>
    /// Creates a retry policy with exponential backoff for transient HTTP failures.
    /// Retries on network failures, 5xx server errors, 429 rate limiting, and IOException.
    /// </summary>
    private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy()
        => Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<IOException>(ex => ex.Message.Contains("forcibly closed") || ex.Message.Contains("transport connection"))
            .OrResult(msg => (int)msg.StatusCode >= 500 || msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests || msg.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var error = outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown";
                    Console.WriteLine($"[Graph API] Retry {retryCount}/3 after {timespan.TotalSeconds:F1}s. Reason: {error}");
                });

    /// <summary>
    /// Creates a circuit breaker policy to prevent cascading failures.
    /// Opens circuit after 5 consecutive failures, stays open for 30 seconds.
    /// </summary>
    private static AsyncCircuitBreakerPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration) =>
                    Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s due to {outcome.Result?.StatusCode}"),
                onReset: () =>
                    Console.WriteLine("Circuit breaker reset"));
}
