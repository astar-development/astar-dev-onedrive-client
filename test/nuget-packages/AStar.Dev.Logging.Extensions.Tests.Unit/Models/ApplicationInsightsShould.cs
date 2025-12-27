// C:\repos\M\astar-dev-logging-extensions\tests\AStar.Dev.Logging.Extensions.Tests.Unit\Models\ApplicationInsightsTest.cs

using AStar.Dev.Logging.Extensions.Models;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(ApplicationInsights))]
public class ApplicationInsightsShould
{
    [Fact]
    public void ApplicationInsights_DefaultConstructor_ShouldInitializeLogLevel()
    {
        var applicationInsights = new ApplicationInsights();

        Assert.NotNull(applicationInsights.LogLevel);
        Assert.IsType<LogLevel>(applicationInsights.LogLevel);
    }

    [Fact]
    public void ApplicationInsights_LogLevel_ShouldDefaultToEmptyValues()
    {
        var applicationInsights = new ApplicationInsights();

        LogLevel logLevel = applicationInsights.LogLevel;

        Assert.Equal(string.Empty, logLevel.Default);
        Assert.Equal(string.Empty, logLevel.MicrosoftAspNetCore);
        Assert.Equal(string.Empty, logLevel.AStar);
    }

    [Fact]
    public void ApplicationInsights_ShouldAllowModifyingLogLevelProperty()
    {
        var applicationInsights = new ApplicationInsights();

        var customLogLevel = new LogLevel { Default = "Information", MicrosoftAspNetCore = "Warning", AStar = "Debug" };
        applicationInsights.LogLevel = customLogLevel;

        Assert.Equal("Information", applicationInsights.LogLevel.Default);
        Assert.Equal("Warning", applicationInsights.LogLevel.MicrosoftAspNetCore);
        Assert.Equal("Debug", applicationInsights.LogLevel.AStar);
    }

    [Fact]
    public void ApplicationInsights_LogLevel_ShouldSupportEdgeCases()
    {
        var applicationInsights = new ApplicationInsights();

        var customLogLevel = new LogLevel { Default = null!, MicrosoftAspNetCore = "", AStar = new('A', 1000) };
        applicationInsights.LogLevel = customLogLevel;

        Assert.Null(applicationInsights.LogLevel.Default);
        Assert.Equal(string.Empty, applicationInsights.LogLevel.MicrosoftAspNetCore);
        Assert.Equal(new('A', 1000), applicationInsights.LogLevel.AStar);
    }
}
