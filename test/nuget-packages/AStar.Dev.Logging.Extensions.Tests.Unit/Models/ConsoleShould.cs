using AStar.Dev.Logging.Extensions.Models;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(Extensions.Models.Console))]
public class ConsoleShould
{
    [Fact]
    public void Console_DefaultValues_ShouldInitializeCorrectly()
    {
        var console = new Extensions.Models.Console();

        console.ShouldNotBeNull();
        console.FormatterName.ShouldNotBeNull();
        console.FormatterName.ShouldBe(string.Empty);
        console.FormatterOptions.ShouldNotBeNull();
        console.FormatterOptions.ShouldBeOfType<FormatterOptions>();
    }

    [Fact]
    public void Console_SetFormatterName_ShouldUpdateValue()
    {
        var console               = new Extensions.Models.Console();
        var expectedFormatterName = "CustomFormatter";

        console.FormatterName = expectedFormatterName;

        console.FormatterName.ShouldBe(expectedFormatterName);
    }

    [Fact]
    public void Console_SetFormatterOptions_ShouldUpdateValue()
    {
        var console = new Extensions.Models.Console();

        var customFormatterOptions = new FormatterOptions
        {
            SingleLine        = true,
            IncludeScopes     = false,
            TimestampFormat   = "yyyy-MM-dd",
            UseUtcTimestamp   = false,
            JsonWriterOptions = new() { Indented = true }
        };

        console.FormatterOptions = customFormatterOptions;

        console.FormatterOptions.ShouldBe(customFormatterOptions);
        console.FormatterOptions.SingleLine.ShouldBeTrue();
        console.FormatterOptions.IncludeScopes.ShouldBeFalse();
        console.FormatterOptions.TimestampFormat.ShouldBe("yyyy-MM-dd");
        console.FormatterOptions.UseUtcTimestamp.ShouldBeFalse();
        console.FormatterOptions.JsonWriterOptions.ShouldNotBeNull();
        console.FormatterOptions.JsonWriterOptions.Indented.ShouldBeTrue();
    }

    [Fact]
    public void FormatterOptions_DefaultValues_ShouldInitializeCorrectly()
    {
        var formatterOptions = new FormatterOptions();

        formatterOptions.ShouldNotBeNull();
        formatterOptions.SingleLine.ShouldBeFalse();
        formatterOptions.IncludeScopes.ShouldBeFalse();
        formatterOptions.TimestampFormat.ShouldBe("HH:mm:ss ");
        formatterOptions.UseUtcTimestamp.ShouldBeTrue();
        formatterOptions.JsonWriterOptions.ShouldNotBeNull();
        formatterOptions.JsonWriterOptions.Indented.ShouldBeFalse();
    }

    [Fact]
    public void FormatterOptions_SetProperties_ShouldUpdateValues()
    {
        var formatterOptions = new FormatterOptions
        {
            SingleLine        = true,
            IncludeScopes     = true,
            TimestampFormat   = "yyyy-MM-dd HH:mm",
            UseUtcTimestamp   = false,
            JsonWriterOptions = new() { Indented = true }
        };

        formatterOptions.SingleLine.ShouldBeTrue();
        formatterOptions.IncludeScopes.ShouldBeTrue();
        formatterOptions.TimestampFormat.ShouldBe("yyyy-MM-dd HH:mm");
        formatterOptions.UseUtcTimestamp.ShouldBeFalse();
        formatterOptions.JsonWriterOptions.ShouldNotBeNull();
        formatterOptions.JsonWriterOptions.Indented.ShouldBeTrue();
    }

    [Fact]
    public void JsonWriterOptions_DefaultValues_ShouldInitializeCorrectly()
    {
        var jsonWriterOptions = new JsonWriterOptions();

        jsonWriterOptions.ShouldNotBeNull();
        jsonWriterOptions.Indented.ShouldBeFalse();
    }

    [Fact]
    public void JsonWriterOptions_SetIndented_ShouldUpdateValue()
    {
        var jsonWriterOptions = new JsonWriterOptions { Indented = true };

        jsonWriterOptions.Indented.ShouldBeTrue();
    }
}
