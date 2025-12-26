using System;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Client.Infrastructure.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit;

public class DatabaseHealthCheckShould
{
    [Fact]
    public async Task ReturnHealthy_WhenDatabaseAccessible()
    {
        var dbContext = Substitute.For<AppDbContext>();
        dbContext.Database.CanConnectAsync(Arg.Any<CancellationToken>()).Returns(true);
        dbContext.DeltaTokens.CountAsync(Arg.Any<CancellationToken>()).Returns(5);
        var check = new DatabaseHealthCheck(dbContext);
        var result = await check.CheckHealthAsync(new HealthCheckContext());
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["deltaTokenCount"].ShouldBe(5);
        result.Data["connectionState"].ShouldBe("connected");
    }

    [Fact]
    public async Task ReturnUnhealthy_WhenDatabaseThrows()
    {
        var dbContext = Substitute.For<AppDbContext>();
        dbContext.Database.CanConnectAsync(Arg.Any<CancellationToken>()).Throws(new Exception("fail"));
        var check = new DatabaseHealthCheck(dbContext);
        var result = await check.CheckHealthAsync(new HealthCheckContext());
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }
}
