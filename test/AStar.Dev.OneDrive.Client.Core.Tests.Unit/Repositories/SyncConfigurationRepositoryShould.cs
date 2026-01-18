using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Core.Tests.Unit.Repositories;

public class SyncConfigurationRepositoryShould
{
    [Fact]
    public async Task GetConfigurationsByAccountIdCorrectly()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        var config1 = new SyncConfiguration(0, "acc1", "/Documents", true, DateTime.UtcNow);
        var config2 = new SyncConfiguration(0, "acc1", "/Photos", false, DateTime.UtcNow);
        var config3 = new SyncConfiguration(0, "acc2", "/Videos", true, DateTime.UtcNow);
        _ = await repository.AddAsync(config1, CancellationToken.None);
        _ = await repository.AddAsync(config2, CancellationToken.None);
        _ = await repository.AddAsync(config3, CancellationToken.None);

        IReadOnlyList<SyncConfiguration> result = await repository.GetByAccountIdAsync("acc1", CancellationToken.None);

        result.Count.ShouldBe(2);
        result.ShouldContain(c => c.FolderPath == "/Documents");
        result.ShouldContain(c => c.FolderPath == "/Photos");
    }

    [Fact]
    public async Task GetSelectedFoldersOnlyForAccount()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        _ = await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Documents", true, DateTime.UtcNow), CancellationToken.None);
        _ = await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Photos", false, DateTime.UtcNow), CancellationToken.None);
        _ = await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Videos", true, DateTime.UtcNow), CancellationToken.None);

        IReadOnlyList<string> result = await repository.GetSelectedFoldersAsync("acc1", CancellationToken.None);

        result.Count.ShouldBe(2);
        result.ShouldContain("/Documents");
        result.ShouldContain("/Videos");
        result.ShouldNotContain("/Photos");
    }

    [Fact]
    public async Task AddConfigurationSuccessfully()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        var config = new SyncConfiguration(0, "acc1", "/Documents", true, DateTime.UtcNow);

        _ = await repository.AddAsync(config, CancellationToken.None);

        IReadOnlyList<SyncConfiguration> result = await repository.GetByAccountIdAsync("acc1", CancellationToken.None);
        result.Count.ShouldBe(1);
        result[0].FolderPath.ShouldBe("/Documents");
        result[0].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateConfigurationSuccessfully()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        var config = new SyncConfiguration(0, "acc1", "/Documents", true, DateTime.UtcNow);
        _ = await repository.AddAsync(config, CancellationToken.None);
        SyncConfiguration saved = (await repository.GetByAccountIdAsync("acc1", CancellationToken.None))[0];

        var updated = new SyncConfiguration(saved.Id, "acc1", "/Documents", false, DateTime.UtcNow);
        await repository.UpdateAsync(updated, CancellationToken.None);

        SyncConfiguration result = (await repository.GetByAccountIdAsync("acc1", CancellationToken.None))[0];
        result.IsSelected.ShouldBeFalse();
    }

    [Fact]
    public async Task ThrowExceptionWhenUpdatingNonExistentConfiguration()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        var config = new SyncConfiguration(999, "acc1", "/Documents", true, DateTime.UtcNow);

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await repository.UpdateAsync(config)
        );

        exception.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task DeleteConfigurationByIdSuccessfully()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        _ = await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Documents", true, DateTime.UtcNow), CancellationToken.None);
        SyncConfiguration saved = (await repository.GetByAccountIdAsync("acc1", CancellationToken.None))[0];

        await repository.DeleteAsync(saved.Id, CancellationToken.None);

        IReadOnlyList<SyncConfiguration> result = await repository.GetByAccountIdAsync("acc1", CancellationToken.None);
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAllConfigurationsForAccount()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        _ = await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Documents", true, DateTime.UtcNow), CancellationToken.None);
        _ = await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Photos", false, DateTime.UtcNow), CancellationToken.None);
        _ = await repository.AddAsync(new SyncConfiguration(0, "acc2", "/Videos", true, DateTime.UtcNow), CancellationToken.None);

        await repository.DeleteByAccountIdAsync("acc1", CancellationToken.None);

        IReadOnlyList<SyncConfiguration> acc1Result = await repository.GetByAccountIdAsync("acc1", CancellationToken.None);
        IReadOnlyList<SyncConfiguration> acc2Result = await repository.GetByAccountIdAsync("acc2", CancellationToken.None);
        acc1Result.ShouldBeEmpty();
        acc2Result.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SaveBatchReplacesExistingConfigurations()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        _ = await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Old1", true, DateTime.UtcNow), CancellationToken.None);
        _ = await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Old2", false, DateTime.UtcNow), CancellationToken.None);

        SyncConfiguration[] newConfigs =
        [
            new SyncConfiguration(0, "acc1", "/New1", true, DateTime.UtcNow),
            new SyncConfiguration(0, "acc1", "/New2", true, DateTime.UtcNow)
        ];
        await repository.SaveBatchAsync("acc1", newConfigs, CancellationToken.None);

        IReadOnlyList<SyncConfiguration> result = await repository.GetByAccountIdAsync("acc1", CancellationToken.None);
        result.Count.ShouldBe(2);
        result.ShouldContain(c => c.FolderPath == "/New1");
        result.ShouldContain(c => c.FolderPath == "/New2");
        result.ShouldNotContain(c => c.FolderPath == "/Old1");
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionForNullAccountId()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);

        _ = await Should.ThrowAsync<ArgumentNullException>(
            async () => await repository.GetByAccountIdAsync(null!)
        );
    }

    private static AppDbContext CreateInMemoryContext()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
