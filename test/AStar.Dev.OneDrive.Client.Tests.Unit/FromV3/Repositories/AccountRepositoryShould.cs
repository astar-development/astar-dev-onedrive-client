using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.FromV3.Repositories;

public class AccountRepositoryShould
{
    [Fact]
    public async Task ReturnEmptyListWhenNoAccountsExist()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new AccountRepository(context);

        IReadOnlyList<AccountInfo> result = await repository.GetAllAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task AddNewAccountSuccessfully()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new AccountRepository(context);
        var account = new AccountInfo("acc1", "John Doe", @"C:\Sync1", true, null, null, false, false, 3, 50, null);

        await repository.AddAsync(account, CancellationToken.None);

        AccountInfo? saved = await repository.GetByIdAsync("acc1", CancellationToken.None);
        _ = saved.ShouldNotBeNull();
        saved.DisplayName.ShouldBe("John Doe");
        saved.LocalSyncPath.ShouldBe(@"C:\Sync1");
        saved.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAllAccountsCorrectly()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new AccountRepository(context);
        await repository.AddAsync(new AccountInfo("acc1", "User 1", @"C:\Sync1", true, null, null, false, false, 3, 50, null), CancellationToken.None);
        await repository.AddAsync(new AccountInfo("acc2", "User 2", @"C:\Sync2", false, null, null, false, false, 3, 50, null), CancellationToken.None);

        IReadOnlyList<AccountInfo> result = await repository.GetAllAsync();

        result.Count.ShouldBe(2);
        result.ShouldContain(a => a.AccountId == "acc1");
        result.ShouldContain(a => a.AccountId == "acc2");
    }

    [Fact]
    public async Task GetAccountByIdCorrectly()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new AccountRepository(context);
        await repository.AddAsync(new AccountInfo("acc1", "User 1", @"C:\Sync1", true, null, "token123", false, false, 3, 50, null), CancellationToken.None);

        AccountInfo? result = await repository.GetByIdAsync("acc1", CancellationToken.None);

        _ = result.ShouldNotBeNull();
        result.AccountId.ShouldBe("acc1");
        result.DisplayName.ShouldBe("User 1");
        result.DeltaToken.ShouldBe("token123");
    }

    [Fact]
    public async Task ReturnNullWhenAccountNotFound()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new AccountRepository(context);

        AccountInfo? result = await repository.GetByIdAsync("nonexistent", CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateExistingAccountSuccessfully()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new AccountRepository(context);
        await repository.AddAsync(new AccountInfo("acc1", "Old Name", @"C:\Sync1", true, null, null, false, false, 3, 50, null), CancellationToken.None);

        var updated = new AccountInfo("acc1", "New Name", @"C:\NewPath", false, DateTime.UtcNow, "newToken", false, false, 3, 50, null);
        await repository.UpdateAsync(updated, CancellationToken.None);

        AccountInfo? result = await repository.GetByIdAsync("acc1", CancellationToken.None);
        _ = result.ShouldNotBeNull();
        result.DisplayName.ShouldBe("New Name");
        result.LocalSyncPath.ShouldBe(@"C:\NewPath");
        result.IsAuthenticated.ShouldBeFalse();
        result.DeltaToken.ShouldBe("newToken");
    }

    [Fact]
    public async Task ThrowExceptionWhenUpdatingNonExistentAccount()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new AccountRepository(context);
        var account = new AccountInfo("nonexistent", "Name", @"C:\Path", true, null, null, false, false, 3, 50, null);

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await repository.UpdateAsync(account)
        );

        exception.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task DeleteAccountSuccessfully()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new AccountRepository(context);
        await repository.AddAsync(new AccountInfo("acc1", "User", @"C:\Sync", true, null, null, false, false, 3, 50, null), CancellationToken.None);

        await repository.DeleteAsync("acc1", CancellationToken.None);

        AccountInfo? result = await repository.GetByIdAsync("acc1", CancellationToken.None);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task NotThrowWhenDeletingNonExistentAccount()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new AccountRepository(context);

        await Should.NotThrowAsync(async () => await repository.DeleteAsync("nonexistent"));
    }

    [Fact]
    public async Task ReturnTrueWhenAccountExists()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new AccountRepository(context);
        await repository.AddAsync(new AccountInfo("acc1", "User", @"C:\Sync", true, null, null, false, false, 3, 50, null), CancellationToken.None);

        var result = await repository.ExistsAsync("acc1", CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ReturnFalseWhenAccountDoesNotExist()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new AccountRepository(context);

        var result = await repository.ExistsAsync("nonexistent", CancellationToken.None);

        result.ShouldBeFalse();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenContextIsNull()
    {
        ArgumentNullException exception = Should.Throw<ArgumentNullException>(
            () => new AccountRepository(null!)
        );

        exception.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionWhenAddingNullAccount()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new AccountRepository(context);

        _ = await Should.ThrowAsync<ArgumentNullException>(
            async () => await repository.AddAsync(null!)
        );
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionWhenUpdatingNullAccount()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new AccountRepository(context);

        _ = await Should.ThrowAsync<ArgumentNullException>(
            async () => await repository.UpdateAsync(null!)
        );
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionWhenGettingByNullId()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new AccountRepository(context);

        _ = await Should.ThrowAsync<ArgumentNullException>(
            async () => await repository.GetByIdAsync(null!)
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
