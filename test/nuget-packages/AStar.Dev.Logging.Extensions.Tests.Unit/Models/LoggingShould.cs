using AStar.Dev.Logging.Extensions.Models;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(Logging))]
public class LoggingShould
{
    // Test for default values of Logging class
    [Fact]
    public void Logging_DefaultValues_ShouldInitializeCorrectly()
    {
        var logging = new Logging();

        Assert.NotNull(logging.Console);
        Assert.NotNull(logging.ApplicationInsights);

        Assert.Equal(string.Empty, logging.Console.FormatterName);
        Assert.NotNull(logging.Console.FormatterOptions);
        Assert.False(logging.Console.FormatterOptions.SingleLine);
        Assert.False(logging.Console.FormatterOptions.IncludeScopes);
        Assert.Equal("HH:mm:ss ", logging.Console.FormatterOptions.TimestampFormat);
        Assert.True(logging.Console.FormatterOptions.UseUtcTimestamp);
        Assert.NotNull(logging.Console.FormatterOptions.JsonWriterOptions);
        Assert.False(logging.Console.FormatterOptions.JsonWriterOptions.Indented);

        Assert.Equal(string.Empty, logging.ApplicationInsights.LogLevel.Default);
        Assert.Equal(string.Empty, logging.ApplicationInsights.LogLevel.MicrosoftAspNetCore);
        Assert.Equal(string.Empty, logging.ApplicationInsights.LogLevel.AStar);
    }

    // Test for Console property assignment
    [Fact]
    public void Logging_Console_ShouldAllowAssignment()
    {
        var newConsole = new Console { FormatterName = "CustomFormatter" };

        var logging = new Logging { Console = newConsole };

        logging.Console.ShouldBeSameAs(newConsole);
        logging.Console.FormatterName.ShouldBe("CustomFormatter");
    }

    // Test for ApplicationInsights property assignment
    [Fact]
    public void Logging_ApplicationInsights_ShouldAllowAssignment()
    {
        var newAppInsights = new ApplicationInsights { LogLevel = new() { Default = "Warning", MicrosoftAspNetCore = "Information", AStar = "Error" } };

        var logging = new Logging { ApplicationInsights = newAppInsights };

        logging.ApplicationInsights.ShouldBeSameAs(newAppInsights);
        logging.ApplicationInsights.LogLevel.Default.ShouldBe("Warning");
        logging.ApplicationInsights.LogLevel.MicrosoftAspNetCore.ShouldBe("Information");
        logging.ApplicationInsights.LogLevel.AStar.ShouldBe("Error");
    }

    // Test for modifications in nested FormatterOptions within Console
    [Fact]
    public void Logging_Console_FormatterOptions_ShouldAllowModification()
    {
        var logging = new Logging();

        logging.Console.FormatterOptions.SingleLine = true;
        logging.Console.FormatterOptions.IncludeScopes = true;
        logging.Console.FormatterOptions.TimestampFormat = "yyyy-MM-dd";
        logging.Console.FormatterOptions.UseUtcTimestamp = false;
        logging.Console.FormatterOptions.JsonWriterOptions.Indented = true;

        Assert.True(logging.Console.FormatterOptions.SingleLine);
        Assert.True(logging.Console.FormatterOptions.IncludeScopes);
        Assert.Equal("yyyy-MM-dd", logging.Console.FormatterOptions.TimestampFormat);
        Assert.False(logging.Console.FormatterOptions.UseUtcTimestamp);
        Assert.True(logging.Console.FormatterOptions.JsonWriterOptions.Indented);
    }

    // Test for modifications in nested LogLevel within ApplicationInsights
    [Fact]
    public void Logging_ApplicationInsights_LogLevel_ShouldAllowModification()
    {
        var logging = new Logging();

        logging.ApplicationInsights.LogLevel.Default = "Debug";
        logging.ApplicationInsights.LogLevel.MicrosoftAspNetCore = "Fatal";
        logging.ApplicationInsights.LogLevel.AStar = "Trace";

        Assert.Equal("Debug", logging.ApplicationInsights.LogLevel.Default);
        Assert.Equal("Fatal", logging.ApplicationInsights.LogLevel.MicrosoftAspNetCore);
        Assert.Equal("Trace", logging.ApplicationInsights.LogLevel.AStar);
    }

    // Test JsonWriterOptions modification within FormatterOptions
    [Fact]
    public void Logging_Console_JsonWriterOptions_ShouldAllowModification()
    {
        var logging = new Logging();

        logging.Console.FormatterOptions.JsonWriterOptions.Indented = true;

        Assert.True(logging.Console.FormatterOptions.JsonWriterOptions.Indented);
    }
}
