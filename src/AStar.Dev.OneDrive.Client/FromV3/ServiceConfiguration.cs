using System.IO.Abstractions;
using AStar.Dev.OneDrive.Client.FromV3.Authentication;
using AStar.Dev.OneDrive.Client.FromV3.OneDriveServices;
using AStar.Dev.OneDrive.Client.FromV3.Repositories;
using AStar.Dev.OneDrive.Client.FromV3.Sync;
using AStar.Dev.OneDrive.Client.SyncConflicts;
using AStar.Dev.OneDrive.Client.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testably.Abstractions;

namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Configures dependency injection services for the application.
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// Configures and returns the service provider with all application services.
    /// </summary>
    /// <returns>Configured service provider.</returns>
    public static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Database
        _ = services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(DatabaseConfiguration.ConnectionString));

        // Repositories
        _ = services.AddScoped<IAccountRepository, AccountRepository>();
        _ = services.AddScoped<ISyncConfigurationRepository, SyncConfigurationRepository>();
        _ = services.AddScoped<IFileMetadataRepository, FileMetadataRepository>();
        _ = services.AddScoped<ISyncConflictRepository, SyncConflictRepository>();
        _ = services.AddScoped<ISyncSessionLogRepository, SyncSessionLogRepository>();
        _ = services.AddScoped<IFileOperationLogRepository, FileOperationLogRepository>();
        _ = services.AddScoped<IDebugLogRepository, DebugLogRepository>();

        // Load authentication configuration
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var authConfig = AuthConfiguration.LoadFromConfiguration(configuration);

        // Authentication - registered as singleton with factory
        _ = services.AddSingleton<IAuthService>(provider =>
            // AuthService.CreateAsync must be called synchronously during startup
            // This is acceptable as it's a one-time initialization cost
            AuthService.CreateAsync(authConfig).GetAwaiter().GetResult());

        // Services
        _ = services.AddSingleton<IFileSystem, RealFileSystem>();
        _ = services.AddSingleton<IFileWatcherService, FileWatcherService>();
        _ = services.AddSingleton<IAutoSyncCoordinator, AutoSyncCoordinator>();
        _ = services.AddSingleton<IAutoSyncSchedulerService, AutoSyncSchedulerService>();
        _ = services.AddScoped<IWindowPreferencesService, WindowPreferencesService>();
        _ = services.AddScoped<IGraphApiClient, GraphApiClient>();
        _ = services.AddScoped<IFolderTreeService, FolderTreeService>();
        _ = services.AddScoped<ISyncSelectionService, SyncSelectionService>();
        _ = services.AddScoped<ILocalFileScanner, LocalFileScanner>();
        _ = services.AddScoped<IRemoteChangeDetector, RemoteChangeDetector>();
        _ = services.AddScoped<IConflictResolver, ConflictResolver>();
        _ = services.AddScoped<ISyncEngine, SyncEngine>();
        _ = services.AddScoped<IDebugLogger, DebugLogger>();

        // ViewModels
        _ = services.AddTransient<AccountManagementViewModel>();
        _ = services.AddTransient<SyncTreeViewModel>();
        _ = services.AddTransient<MainWindowViewModel>();
        _ = services.AddTransient<ConflictResolutionViewModel>();
        _ = services.AddTransient<SyncProgressViewModel>();
        _ = services.AddTransient<UpdateAccountDetailsViewModel>();

        // Logging
        _ = services.AddLogging(builder =>
        {
            _ = builder.AddConsole();
            _ = builder.SetMinimumLevel(LogLevel.Information);
        });

        // Background Services
        _ = services.AddHostedService<LogCleanupBackgroundService>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Ensures the database is created and migrations are applied.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public static void EnsureDatabaseCreated(ServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            context.Database.Migrate();
        }
        catch
        {
            // If EnsureCreated fails (e.g. due to existing but outdated database), apply migrations
        }
    }
}
