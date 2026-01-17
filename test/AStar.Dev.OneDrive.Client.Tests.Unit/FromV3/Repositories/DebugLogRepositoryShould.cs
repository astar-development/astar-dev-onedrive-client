using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.FromV3.Repositories;

public class DebugLogRepositoryShould
{
    [Fact]
    public async Task GetByAccountIdWithPagingReturnsCorrectRecords()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new DebugLogRepository(context);
        await SeedDebugLogsAsync(context, "acc1", 10);

        IReadOnlyList<DebugLogEntry> result = await repository.GetByAccountIdAsync("acc1", 5, 0, CancellationToken.None);

        result.Count.ShouldBe(5);
    }

    [Fact]
    public async Task GetByAccountIdWithPagingSkipsCorrectRecords()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new DebugLogRepository(context);
        await SeedDebugLogsAsync(context, "acc1", 10);

        IReadOnlyList<DebugLogEntry> result = await repository.GetByAccountIdAsync("acc1", 5, 5, CancellationToken.None);

        result.Count.ShouldBe(5);
    }

    [Fact]
    public async Task GetByAccountIdReturnsAllRecordsForAccount()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new DebugLogRepository(context);
        await SeedDebugLogsAsync(context, "acc1", 15);
        await SeedDebugLogsAsync(context, "acc2", 5);

        IReadOnlyList<DebugLogEntry> result = await repository.GetByAccountIdAsync("acc1", CancellationToken.None);

        result.Count.ShouldBe(15);
        result.All(log => log.AccountId == "acc1").ShouldBeTrue();
    }

    [Fact]
    public async Task GetByAccountIdReturnsRecordsOrderedByTimestampDescending()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new DebugLogRepository(context);

        // Add logs with different timestamps
        _ = context.DebugLogs.Add(new DebugLogEntity
        {
            AccountId = "acc1",
            TimestampUtc = DateTime.UtcNow.AddHours(-2),
            LogLevel = "Info",
            Source = "Test",
            Message = "Oldest"
        });
        _ = context.DebugLogs.Add(new DebugLogEntity
        {
            AccountId = "acc1",
            TimestampUtc = DateTime.UtcNow,
            LogLevel = "Info",
            Source = "Test",
            Message = "Newest"
        });
        _ = context.DebugLogs.Add(new DebugLogEntity
        {
            AccountId = "acc1",
            TimestampUtc = DateTime.UtcNow.AddHours(-1),
            LogLevel = "Info",
            Source = "Test",
            Message = "Middle"
        });
        _ = await context.SaveChangesAsync();

        IReadOnlyList<DebugLogEntry> result = await repository.GetByAccountIdAsync("acc1", CancellationToken.None);

        result[0].Message.ShouldBe("Newest");
        result[1].Message.ShouldBe("Middle");
        result[2].Message.ShouldBe("Oldest");
    }

    [Fact]
    public async Task DeleteByAccountIdRemovesOnlySpecifiedAccountLogs()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new DebugLogRepository(context);
        await SeedDebugLogsAsync(context, "acc1", 5);
        await SeedDebugLogsAsync(context, "acc2", 3);

        await repository.DeleteByAccountIdAsync("acc1", CancellationToken.None);

        IReadOnlyList<DebugLogEntry> acc1Logs = await repository.GetByAccountIdAsync("acc1", CancellationToken.None);
        IReadOnlyList<DebugLogEntry> acc2Logs = await repository.GetByAccountIdAsync("acc2", CancellationToken.None);
        acc1Logs.ShouldBeEmpty();
        acc2Logs.Count.ShouldBe(3);
    }

    [Fact]
    public async Task DeleteOlderThanRemovesOldRecords()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new DebugLogRepository(context);

        DateTime cutoff = DateTime.UtcNow.AddDays(-7);

        _ = context.DebugLogs.Add(new DebugLogEntity
        {
            AccountId = "acc1",
            TimestampUtc = cutoff.AddDays(-1),
            LogLevel = "Info",
            Source = "Test",
            Message = "Old"
        });
        _ = context.DebugLogs.Add(new DebugLogEntity
        {
            AccountId = "acc1",
            TimestampUtc = cutoff.AddDays(1),
            LogLevel = "Info",
            Source = "Test",
            Message = "Recent"
        });
        _ = await context.SaveChangesAsync();

        await repository.DeleteOlderThanAsync(cutoff, CancellationToken.None);

        IReadOnlyList<DebugLogEntry> result = await repository.GetByAccountIdAsync("acc1", CancellationToken.None);
        result.Count.ShouldBe(1);
        result[0].Message.ShouldBe("Recent");
    }

    [Fact]
    public async Task GetByAccountIdReturnsEmptyListWhenNoRecordsExist()
    {
        using AppDbContext context = CreateInMemoryContext();
        var repository = new DebugLogRepository(context);

        IReadOnlyList<DebugLogEntry> result = await repository.GetByAccountIdAsync("nonexistent", CancellationToken.None);

        result.ShouldBeEmpty();
    }

    private static AppDbContext CreateInMemoryContext()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task SeedDebugLogsAsync(AppDbContext context, string accountId, int count)
    {
        for(var i = 0; i < count; i++)
        {
            _ = context.DebugLogs.Add(new DebugLogEntity
            {
                AccountId = accountId,
                TimestampUtc = DateTime.UtcNow.AddMinutes(-i),
                LogLevel = "Info",
                Source = $"Test.Method{i}",
                Message = $"Log message {i}"
            });
        }

        _ = await context.SaveChangesAsync();
    }
}
