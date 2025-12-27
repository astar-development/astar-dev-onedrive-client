using AStar.Dev.Utilities;
using Microsoft.ApplicationInsights.Extensibility;
using NSubstitute;
using Serilog;

namespace AStar.Dev.Logging.Extensions.Tests.Unit;

[TestSubject(typeof(SerilogConfigure))]
public class SerilogConfigureShould
{
    [Fact]
    public void ConfigureTheLoggerWhenParametersAreValid()
    {
        var loggerConfiguration = new LoggerConfiguration();
        var configurationMock   = new ConfigurationBuilder();
        configurationMock.AddInMemoryCollection();
        var telemetryConfiguration = new TelemetryConfiguration();

        LoggerConfiguration result = loggerConfiguration.Configure(configurationMock.Build(), telemetryConfiguration);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<LoggerConfiguration>();
        result.WriteTo.ShouldNotBeNull();
        result.ReadFrom.ShouldNotBeNull();
        result.ToJson().ShouldMatchApproved();
    }

    [Fact]
    public void ThrowNullReferenceExceptionWhenLoggerConfigurationIsNull()
    {
        LoggerConfiguration? loggerConfiguration = null;
        var                  configurationMock   = new ConfigurationBuilder();
        configurationMock.AddInMemoryCollection();
        var telemetryConfiguration = new TelemetryConfiguration();

        Should.Throw<NullReferenceException>(() => loggerConfiguration!.Configure(configurationMock.Build(), telemetryConfiguration));
    }

    [Fact]
    public void ThrowNullReferenceExceptionWhenConfigurationIsNull()
    {
        var             loggerConfiguration    = new LoggerConfiguration();
        IConfiguration? configuration          = null;
        var             telemetryConfiguration = new TelemetryConfiguration();

        Should.Throw<NullReferenceException>(() => loggerConfiguration.Configure(configuration!, telemetryConfiguration));
    }

    [Fact]
    public void ThrowInvalidOperationExceptionWhenTelemetryConfigurationIsNull()
    {
        var                     loggerConfiguration    = new LoggerConfiguration();
        IConfiguration configurationMock      = Substitute.For<IConfiguration>();
        TelemetryConfiguration? telemetryConfiguration = null;

        Should.Throw<InvalidOperationException>(() => loggerConfiguration.Configure(configurationMock, telemetryConfiguration!));
    }

    [Fact]
    public void HandleEmptyConfiguration()
    {
        var loggerConfiguration = new LoggerConfiguration();
        var configurationMock   = new ConfigurationBuilder();
        configurationMock.AddInMemoryCollection();
        var telemetryConfiguration = new TelemetryConfiguration();

        LoggerConfiguration result = loggerConfiguration.Configure(configurationMock.Build(), telemetryConfiguration);

        result.ShouldNotBeNull();
    }

    [Fact]
    public void HandleNullSectionsInsideConfiguration()
    {
        var loggerConfiguration = new LoggerConfiguration();
        var configurationMock   = new ConfigurationBuilder();
        configurationMock.AddInMemoryCollection();
        var telemetryConfiguration = new TelemetryConfiguration();

        LoggerConfiguration result = loggerConfiguration.Configure(configurationMock.Build(), telemetryConfiguration);

        result.ShouldNotBeNull();
    }
}
