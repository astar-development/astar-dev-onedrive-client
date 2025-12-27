using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Client.Infrastructure.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit;

public class DatabaseHealthCheckShould
{
    [Fact]
    public async Task ReturnHealthyStatusWhenDatabaseIsAccessible()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
        await using var dbContext = new AppDbContext(options);
        dbContext.DeltaTokens.Add(new DeltaToken("MockId", "MockToken", DateTime.UtcNow));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var check = new DatabaseHealthCheck(dbContext);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["deltaTokenCount"].ShouldBe(1);
        result.Data["connectionState"].ShouldBe("connected");
    }

    [Fact]
    public async Task ReturnUnhealthyStatusWhenDatabaseThrows()
    {
        AppDbContext dbContext = Substitute.For<AppDbContext>();
        DatabaseFacade dbFacade = Substitute.For<DatabaseFacade>(dbContext);
        dbContext.Database.Returns(dbFacade);
        dbFacade.CanConnectAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new Exception("fail")));

        var check = new DatabaseHealthCheck(dbContext);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }
}
