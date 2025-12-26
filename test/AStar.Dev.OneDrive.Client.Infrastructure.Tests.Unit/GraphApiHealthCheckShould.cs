using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Infrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit;

public class GraphApiHealthCheckShould
{
    [Fact]
    public async Task ReturnDegradedStatusWhenUserIsNotSignedIn()
    {
        IAuthService auth = Substitute.For<IAuthService>();
        auth.IsSignedIn.Returns(false);

        var check = new GraphApiHealthCheck(auth);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task ReturnHealthyStatusWhenUserIsSignedInAndTokenAcquired()
    {
        IAuthService auth = Substitute.For<IAuthService>();
        auth.IsSignedIn.Returns(true);
        auth.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("token");

        var check = new GraphApiHealthCheck(auth);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["authenticated"].ShouldBe(true);
        result.Data["tokenAcquired"].ShouldBe(true);
    }

    [Fact]
    public async Task ReturnUnhealthyStatusWhenHttpRequestExceptionIsThrown()
    {
        IAuthService auth = Substitute.For<IAuthService>();
        auth.IsSignedIn.Returns(true);
        auth.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Throws(new HttpRequestException());

        var check = new GraphApiHealthCheck(auth);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Data["errorType"].ShouldBe("NetworkError");
    }

    [Fact]
    public async Task ReturnUnhealthyStatusWhenUnexpectedExceptionIsThrown()
    {
        IAuthService auth = Substitute.For<IAuthService>();
        auth.IsSignedIn.Returns(true);
        auth.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Throws(new Exception("fail"));

        var check = new GraphApiHealthCheck(auth);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }
}
