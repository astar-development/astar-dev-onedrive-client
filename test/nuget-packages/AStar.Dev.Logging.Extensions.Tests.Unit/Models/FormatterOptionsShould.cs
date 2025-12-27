using AStar.Dev.Logging.Extensions.Models;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(FormatterOptions))]
public class FormatterOptionsShould
{
    [Fact]
    public void SingleLine_ShouldDefaultToFalse()
    {
        var options = new FormatterOptions();

        var result = options.SingleLine;

        result.ShouldBeFalse();
    }

    [Fact]
    public void SingleLine_ShouldSetAndGetCorrectly()
    {
        var options = new FormatterOptions { SingleLine = true };

        options.SingleLine.ShouldBeTrue();
    }

    [Fact]
    public void IncludeScopes_ShouldDefaultToFalse()
    {
        var options = new FormatterOptions();

        var result = options.IncludeScopes;

        result.ShouldBeFalse();
    }

    [Fact]
    public void IncludeScopes_ShouldSetAndGetCorrectly()
    {
        var options = new FormatterOptions { IncludeScopes = true };

        options.IncludeScopes.ShouldBeTrue();
    }

    [Fact]
    public void TimestampFormat_ShouldDefaultTo_HH_mm_ss_Space()
    {
        var options = new FormatterOptions();

        var result = options.TimestampFormat;

        result.ShouldBe("HH:mm:ss ");
    }

    [Fact]
    public void TimestampFormat_ShouldSetAndGetCorrectly()
    {
        var options   = new FormatterOptions();
        var newFormat = "yyyy-MM-dd";

        options.TimestampFormat = newFormat;

        options.TimestampFormat.ShouldBe(newFormat);
    }

    [Fact]
    public void UseUtcTimestamp_ShouldDefaultToTrue()
    {
        var options = new FormatterOptions();

        var result = options.UseUtcTimestamp;

        result.ShouldBeTrue();
    }

    [Fact]
    public void UseUtcTimestamp_ShouldSetAndGetCorrectly()
    {
        var options = new FormatterOptions { UseUtcTimestamp = false };

        options.UseUtcTimestamp.ShouldBeFalse();
    }

    [Fact]
    public void JsonWriterOptions_ShouldDefaultTo_NotNull()
    {
        var options = new FormatterOptions();

        JsonWriterOptions result = options.JsonWriterOptions;

        result.ShouldNotBeNull();
    }

    [Fact]
    public void JsonWriterOptions_Indented_ShouldSetAndGetCorrectly()
    {
        var options = new FormatterOptions { JsonWriterOptions = { Indented = true } };

        options.JsonWriterOptions.Indented.ShouldBeTrue();
    }
}
