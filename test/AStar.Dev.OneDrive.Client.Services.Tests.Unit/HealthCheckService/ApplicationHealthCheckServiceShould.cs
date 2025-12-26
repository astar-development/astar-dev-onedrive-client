using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit.HealthCheckService;

public class ApplicationHealthCheckServiceShould
{
    [Fact]
    public async Task Return_the_health_report_from_the_underlying_service_when_GetHealthAsync_is_called()
    {
        var expectedReport = new HealthReport(new Dictionary<string, HealthReportEntry>(), TimeSpan.Zero);
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService healthCheckService = Substitute.For<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
        healthCheckService.CheckHealthAsync(Arg.Any<CancellationToken>()).Returns(expectedReport);
        var sut = new ApplicationHealthCheckService(healthCheckService);

        HealthReport result = await sut.GetHealthAsync(TestContext.Current.CancellationToken);

        result.ShouldBe(expectedReport);
    }

    [Fact]
    public async Task Return_null_when_the_health_check_name_is_not_found()
    {
        var report = new HealthReport(new Dictionary<string, HealthReportEntry>(), TimeSpan.Zero);
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService healthCheckService = Substitute.For<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
        healthCheckService.CheckHealthAsync(Arg.Any<CancellationToken>()).Returns(report);
        var sut = new ApplicationHealthCheckService(healthCheckService);

        HealthCheckResult? result = await sut.GetHealthCheckAsync("missing", TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task Return_a_healthy_result_when_the_entry_is_healthy()
    {
        var entry = new HealthReportEntry(HealthStatus.Healthy, "desc", TimeSpan.Zero, null, new Dictionary<string, object>());
        var report = new HealthReport(new Dictionary<string, HealthReportEntry> { { "foo", entry } }, TimeSpan.Zero);
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService healthCheckService = Substitute.For<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
        healthCheckService.CheckHealthAsync(Arg.Any<CancellationToken>()).Returns(report);
        var sut = new ApplicationHealthCheckService(healthCheckService);

        HealthCheckResult? result = await sut.GetHealthCheckAsync("foo", TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Value.Status.ShouldBe(HealthStatus.Healthy);
        result.Value.Description.ShouldBe("desc");
    }

    [Fact]
    public async Task Return_an_unhealthy_result_when_the_entry_is_unhealthy()
    {
        var entry = new HealthReportEntry(HealthStatus.Unhealthy, "desc", TimeSpan.Zero, new Exception("fail"), new Dictionary<string, object>());
        var report = new HealthReport(new Dictionary<string, HealthReportEntry> { { "bar", entry } }, TimeSpan.Zero);
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService healthCheckService = Substitute.For<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
        healthCheckService.CheckHealthAsync(Arg.Any<CancellationToken>()).Returns(report);
        var sut = new ApplicationHealthCheckService(healthCheckService);

        HealthCheckResult? result = await sut.GetHealthCheckAsync("bar", TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Value.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Value.Description.ShouldBe("desc");
        result.Value.Exception?.Message.ShouldBe("fail");
    }
}
