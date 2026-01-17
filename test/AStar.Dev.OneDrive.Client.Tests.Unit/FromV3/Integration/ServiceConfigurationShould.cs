using AStar.Dev.OneDrive.Client.FromV3;
using AStar.Dev.OneDrive.Client.FromV3.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.FromV3.Integration;

public class ServiceConfigurationShould
{
    [Fact]
    public void ConfigureAllServicesCorrectly()
    {
        ServiceProvider serviceProvider = ServiceConfiguration.ConfigureServices();

        _ = serviceProvider.ShouldNotBeNull();
    }

    [Fact]
    public void ResolveAccountRepositorySuccessfully()
    {
        using ServiceProvider serviceProvider = ServiceConfiguration.ConfigureServices();

        IAccountRepository? repository = serviceProvider.GetService<IAccountRepository>();

        _ = repository.ShouldNotBeNull();
        _ = repository.ShouldBeOfType<AccountRepository>();
    }

    [Fact]
    public void ResolveSyncConfigurationRepositorySuccessfully()
    {
        using ServiceProvider serviceProvider = ServiceConfiguration.ConfigureServices();

        ISyncConfigurationRepository? repository = serviceProvider.GetService<ISyncConfigurationRepository>();

        _ = repository.ShouldNotBeNull();
        _ = repository.ShouldBeOfType<SyncConfigurationRepository>();
    }

    [Fact]
    public void ResolveFileMetadataRepositorySuccessfully()
    {
        using ServiceProvider serviceProvider = ServiceConfiguration.ConfigureServices();

        IFileMetadataRepository? repository = serviceProvider.GetService<IFileMetadataRepository>();

        _ = repository.ShouldNotBeNull();
        _ = repository.ShouldBeOfType<FileMetadataRepository>();
    }

    [Fact]
    public void ResolveWindowPreferencesServiceSuccessfully()
    {
        using ServiceProvider serviceProvider = ServiceConfiguration.ConfigureServices();

        IWindowPreferencesService? service = serviceProvider.GetService<IWindowPreferencesService>();

        _ = service.ShouldNotBeNull();
        _ = service.ShouldBeOfType<WindowPreferencesService>();
    }

    [Fact]
    public void CreateScopedInstancesForRepositories()
    {
        using ServiceProvider serviceProvider = ServiceConfiguration.ConfigureServices();

        IAccountRepository? repo1;
        IAccountRepository? repo2;

        using(IServiceScope scope1 = serviceProvider.CreateScope())
        {
            repo1 = scope1.ServiceProvider.GetService<IAccountRepository>();
        }

        using(IServiceScope scope2 = serviceProvider.CreateScope())
        {
            repo2 = scope2.ServiceProvider.GetService<IAccountRepository>();
        }

        _ = repo1.ShouldNotBeNull();
        _ = repo2.ShouldNotBeNull();
        repo1.ShouldNotBeSameAs(repo2);
    }

    [Fact]
    public void EnsureDatabaseCreatedDoesNotThrow()
    {
        using ServiceProvider serviceProvider = ServiceConfiguration.ConfigureServices();

        Should.NotThrow(() => ServiceConfiguration.EnsureDatabaseCreated(serviceProvider));
    }
}
