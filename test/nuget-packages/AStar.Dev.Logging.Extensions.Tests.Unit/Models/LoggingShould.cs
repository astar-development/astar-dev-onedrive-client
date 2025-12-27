using AStar.Dev.Logging.Extensions.Models;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(Extensions.Models.Logging))]
public class LoggingShould
{
    // Test for default values of Logging class
    [Fact]
    public void Logging_DefaultValues_ShouldInitializeCorrectly()
    {
        var logging = new Extensions.Models.Logging();

        logging.Console.ShouldNotBeNull();
        logging.ApplicationInsights.ShouldNotBeNull();

        logging.Console.FormatterName.ShouldBe(string.Empty);
        logging.Console.FormatterOptions.ShouldNotBeNull();
        logging.Console.FormatterOptions.SingleLine.ShouldBeFalse();
        logging.Console.FormatterOptions.IncludeScopes.ShouldBeFalse();
        logging.Console.FormatterOptions.TimestampFormat.ShouldBe("HH:mm:ss ");
        logging.Console.FormatterOptions.UseUtcTimestamp.ShouldBeTrue();
        logging.Console.FormatterOptions.JsonWriterOptions.ShouldNotBeNull();
        logging.Console.FormatterOptions.JsonWriterOptions.Indented.ShouldBeFalse();

        logging.ApplicationInsights.LogLevel.Default.ShouldBe(string.Empty);
        logging.ApplicationInsights.LogLevel.MicrosoftAspNetCore.ShouldBe(string.Empty);
        logging.ApplicationInsights.LogLevel.AStar.ShouldBe(string.Empty);
    }

    // Test for Console property assignment
    [Fact]
    public void Logging_Console_ShouldAllowAssignment()
    {
        var newConsole = new Extensions.Models.Console { FormatterName = "CustomFormatter" };

        var logging = new Extensions.Models.Logging { Console = newConsole };

        logging.Console.ShouldBeSameAs(newConsole);
        logging.Console.FormatterName.ShouldBe("CustomFormatter");
    }

    // Test for ApplicationInsights property assignment
    [Fact]
    public void Logging_ApplicationInsights_ShouldAllowAssignment()
    {
        var newAppInsights = new ApplicationInsights { LogLevel = new() { Default = "Warning", MicrosoftAspNetCore = "Information", AStar = "Error" } };

        var logging = new Extensions.Models.Logging { ApplicationInsights = newAppInsights };

        logging.ApplicationInsights.ShouldBeSameAs(newAppInsights);
        logging.ApplicationInsights.LogLevel.Default.ShouldBe("Warning");
        logging.ApplicationInsights.LogLevel.MicrosoftAspNetCore.ShouldBe("Information");
        logging.ApplicationInsights.LogLevel.AStar.ShouldBe("Error");
    }

    // Test for modifications in nested FormatterOptions within Console
    [Fact]
    public void Logging_Console_FormatterOptions_ShouldAllowModification()
    {
        var logging = new Extensions.Models.Logging();

        logging.Console.FormatterOptions.SingleLine = true;
        logging.Console.FormatterOptions.IncludeScopes = true;
        logging.Console.FormatterOptions.TimestampFormat = "yyyy-MM-dd";
        logging.Console.FormatterOptions.UseUtcTimestamp = false;
        logging.Console.FormatterOptions.JsonWriterOptions.Indented = true;

        logging.Console.FormatterOptions.SingleLine.ShouldBeTrue();
        logging.Console.FormatterOptions.IncludeScopes.ShouldBeTrue();
        logging.Console.FormatterOptions.TimestampFormat.ShouldBe("yyyy-MM-dd");
        logging.Console.FormatterOptions.UseUtcTimestamp.ShouldBeFalse();
        logging.Console.FormatterOptions.JsonWriterOptions.Indented.ShouldBeTrue();
    }

    // Test for modifications in nested LogLevel within ApplicationInsights
    [Fact]
    public void Logging_ApplicationInsights_LogLevel_ShouldAllowModification()
    {
        var logging = new Extensions.Models.Logging();

        logging.ApplicationInsights.LogLevel.Default = "Debug";
        logging.ApplicationInsights.LogLevel.MicrosoftAspNetCore = "Fatal";
        logging.ApplicationInsights.LogLevel.AStar = "Trace";

        logging.ApplicationInsights.LogLevel.Default.ShouldBe("Debug");
        logging.ApplicationInsights.LogLevel.MicrosoftAspNetCore.ShouldBe("Fatal");
        logging.ApplicationInsights.LogLevel.AStar.ShouldBe("Trace");
    }

    // Test JsonWriterOptions modification within FormatterOptions
    [Fact]
    public void Logging_Console_JsonWriterOptions_ShouldAllowModification()
    {
        var logging = new Extensions.Models.Logging();

        logging.Console.FormatterOptions.JsonWriterOptions.Indented = true;

        logging.Console.FormatterOptions.JsonWriterOptions.Indented.ShouldBeTrue();
    }
}
