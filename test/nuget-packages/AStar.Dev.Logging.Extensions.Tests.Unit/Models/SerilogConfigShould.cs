namespace AStar.Dev.Logging.Extensions.Models;

[TestSubject(typeof(SerilogConfig))]
public class SerilogConfigShould
{
    [Fact]
    public void SerilogConfig_ShouldInitializeWithDefaultValues()
    {
        var config = new SerilogConfig();

        config.Serilog.ShouldNotBeNull();
        config.Logging.ShouldNotBeNull();
    }

    [Fact]
    public void SerilogConfig_ShouldAllowSettingProperties()
    {
        var serilogConfig = new SerilogConfig();

        var serilog = new Serilog
        {
            Enrich       = ["ThreadId", "MachineName"],
            WriteTo      = [new WriteTo { Name                                              = "File", Args             = new() { ServerUrl = "http://localhost" } }],
            MinimumLevel = new() { Default = "Information", Override = new() { MicrosoftAspNetCore = "Warning", SystemNetHttp = "Error", AStar = "Debug" } }
        };

        var logging = new Logging
        {
            Console = new()
            {
                FormatterName = "default",
                FormatterOptions = new()
                {
                    SingleLine        = true,
                    IncludeScopes     = false,
                    TimestampFormat   = "yyyy-MM-dd",
                    UseUtcTimestamp   = false,
                    JsonWriterOptions = new()
                }
            },
            ApplicationInsights = new() { LogLevel = new() { Default = "Debug", MicrosoftAspNetCore = "Trace", AStar = "Error" } }
        };

        serilogConfig.Serilog = serilog;
        serilogConfig.Logging = logging;

        serilogConfig.Serilog.ShouldBeEquivalentTo(serilog);
        serilogConfig.Logging.ShouldBeEquivalentTo(logging);
    }

    [Fact]
    public void Serilog_ShouldInitializeWithDefaultValues()
    {
        var serilog = new Serilog();

        serilog.Enrich.ShouldBeEmpty();
        serilog.WriteTo.ShouldNotBeEmpty();
        serilog.MinimumLevel.ShouldNotBeNull();
        serilog.MinimumLevel.Default.ShouldBeEmpty();
        serilog.MinimumLevel.Override.ShouldNotBeNull();
    }

    [Fact]
    public void Logging_ShouldInitializeWithDefaultValues()
    {
        var logging = new Logging();

        logging.Console.ShouldNotBeNull();
        logging.ApplicationInsights.ShouldNotBeNull();
    }

    [Fact]
    public void WriteTo_ShouldInitializeWithDefaultValues()
    {
        var writeTo = new WriteTo();

        writeTo.Name.ShouldBeEmpty();
        writeTo.Args.ShouldNotBeNull();
        writeTo.Args.ServerUrl.ShouldBeEmpty();
    }

    [Fact]
    public void MinimumLevel_ShouldInitializeWithDefaultValues()
    {
        var minimumLevel = new MinimumLevel();

        minimumLevel.Default.ShouldBeEmpty();
        minimumLevel.Override.ShouldNotBeNull();
        minimumLevel.Override.MicrosoftAspNetCore.ShouldBeEmpty();
        minimumLevel.Override.SystemNetHttp.ShouldBeEmpty();
        minimumLevel.Override.AStar.ShouldBeEmpty();
    }

    [Fact]
    public void Console_ShouldInitializeWithDefaultValues()
    {
        var console = new Console();

        console.FormatterName.ShouldBeEmpty();
        console.FormatterOptions.ShouldNotBeNull();
        console.FormatterOptions.SingleLine.ShouldBeFalse();
        console.FormatterOptions.IncludeScopes.ShouldBeFalse();
        console.FormatterOptions.TimestampFormat.ShouldBe("HH:mm:ss ");
        console.FormatterOptions.UseUtcTimestamp.ShouldBeTrue();
        console.FormatterOptions.JsonWriterOptions.ShouldNotBeNull();
    }

    [Fact]
    public void ApplicationInsights_ShouldInitializeWithDefaultValues()
    {
        var applicationInsights = new ApplicationInsights();

        applicationInsights.LogLevel.ShouldNotBeNull();
        applicationInsights.LogLevel.Default.ShouldBeEmpty();
        applicationInsights.LogLevel.MicrosoftAspNetCore.ShouldBeEmpty();
        applicationInsights.LogLevel.AStar.ShouldBeEmpty();
    }

    [Fact]
    public void FormatterOptions_ShouldInitializeWithDefaultValues()
    {
        var formatterOptions = new FormatterOptions();

        formatterOptions.SingleLine.ShouldBeFalse();
        formatterOptions.IncludeScopes.ShouldBeFalse();
        formatterOptions.TimestampFormat.ShouldBe("HH:mm:ss ");
        formatterOptions.UseUtcTimestamp.ShouldBeTrue();
        formatterOptions.JsonWriterOptions.ShouldNotBeNull();
    }

    [Fact]
    public void JsonWriterOptions_ShouldInitializeSuccessfully()
    {
        var jsonWriterOptions = new JsonWriterOptions();

        jsonWriterOptions.ShouldNotBeNull();
    }
}
