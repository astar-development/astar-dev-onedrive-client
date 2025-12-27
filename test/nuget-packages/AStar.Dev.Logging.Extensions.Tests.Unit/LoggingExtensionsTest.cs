using NSubstitute;

namespace AStar.Dev.Logging.Extensions.Tests.Unit;

/// <summary>
///     Tests for <see cref="LoggingExtensions" />.
/// </summary>
[TestSubject(typeof(LoggingExtensions))]
public class LoggingExtensionsTest
{
    [Fact]
    public void AddSerilogLogging_WebApplicationBuilder_ValidExternalSettingsFile_ShouldConfigureSerilog()
    {
        IConfigurationRoot configMock = new ConfigurationBuilder().AddInMemoryCollection().Build();
        WebApplicationBuilder builder    = WebApplication.CreateBuilder();
        builder.Configuration.AddConfiguration(configMock);
        var externalSettingsFile = "testsettings.json";

        ITelemetryClient telemetryClient = Substitute.For<ITelemetryClient>();
        IServiceProvider telemetryMock   = Substitute.For<IServiceProvider>();
        telemetryMock.GetService(typeof(ITelemetryClient)).Returns(telemetryClient);
        builder.Services.AddSingleton(telemetryMock);

        WebApplicationBuilder configuredBuilder = builder.AddSerilogLogging(externalSettingsFile);

        configuredBuilder.ShouldNotBeNull();
        configuredBuilder.ShouldBe(builder);
    }

    [Fact]
    public void AddSerilogLogging_WebApplicationBuilder_NullExternalSettingsFile_ShouldNotLoadJsonFile()
    {
        IConfiguration configMock = Substitute.For<IConfiguration>();
        WebApplicationBuilder builder    = WebApplication.CreateBuilder();
        builder.Configuration.AddConfiguration(configMock);

        string? externalSettingsFile = null;

        ITelemetryClient telemetryClient = Substitute.For<ITelemetryClient>();
        IServiceProvider telemetryMock   = Substitute.For<IServiceProvider>();
        telemetryMock.GetService(typeof(ITelemetryClient)).Returns(telemetryClient);
        builder.Services.AddSingleton(telemetryMock);

        WebApplicationBuilder configuredBuilder = builder.AddSerilogLogging(externalSettingsFile!);

        configuredBuilder.ShouldNotBeNull();
        configuredBuilder.ShouldBe(builder);
    }

    [Fact]
    public void AddSerilogLogging_HostApplicationBuilder_ValidExternalSettingsFile_ShouldConfigureSerilog()
    {
        IConfigurationRoot configMock = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var builder    = new HostApplicationBuilder();
        builder.Configuration.AddConfiguration(configMock);
        var externalSettingsFile = "testsettings.json";

        ITelemetryClient telemetryClient = Substitute.For<ITelemetryClient>();
        IServiceProvider telemetryMock   = Substitute.For<IServiceProvider>();
        telemetryMock.GetService(typeof(ITelemetryClient)).Returns(telemetryClient);
        builder.Services.AddSingleton(telemetryMock);

        HostApplicationBuilder configuredBuilder = builder.AddSerilogLogging(externalSettingsFile);

        configuredBuilder.ShouldNotBeNull();
        configuredBuilder.ShouldBe(builder);
    }

    [Fact]
    public void AddSerilogLogging_HostApplicationBuilder_NullExternalSettingsFile_ShouldNotLoadJsonFile()
    {
        IConfiguration configMock = Substitute.For<IConfiguration>();
        var builder    = new HostApplicationBuilder();
        builder.Configuration.AddConfiguration(configMock);
        string? externalSettingsFile = null;

        ITelemetryClient telemetryClient = Substitute.For<ITelemetryClient>();
        IServiceProvider telemetryMock   = Substitute.For<IServiceProvider>();
        telemetryMock.GetService(typeof(ITelemetryClient)).Returns(telemetryClient);
        builder.Services.AddSingleton(telemetryMock);

        HostApplicationBuilder configuredBuilder = builder.AddSerilogLogging(externalSettingsFile!);

        configuredBuilder.ShouldNotBeNull();
        configuredBuilder.ShouldBe(builder);
    }
}
