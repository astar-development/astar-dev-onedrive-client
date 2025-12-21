using AStar.Dev.Utilities;

namespace AStar.Dev.Logging.Extensions.Models;

[TestSubject(typeof(LogLevel))]
public class LogLevelShould
{
    [Fact]
    public void Default_ShouldHaveInitialValue_EmptyString()
    {
        var logLevel = new LogLevel();

        var result = logLevel.Default;

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Default_ShouldAllowSettingValue()
    {
        var logLevel      = new LogLevel();
        var expectedValue = "Info";

        logLevel.Default = expectedValue;
        var result = logLevel.Default;

        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public void MicrosoftAspNetCore_ShouldHaveInitialValue_EmptyString()
    {
        var logLevel = new LogLevel();

        var result = logLevel.MicrosoftAspNetCore;

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void MicrosoftAspNetCore_ShouldAllowSettingValue()
    {
        var logLevel      = new LogLevel();
        var expectedValue = "Warning";

        logLevel.MicrosoftAspNetCore = expectedValue;
        var result = logLevel.MicrosoftAspNetCore;

        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public void AStar_ShouldHaveInitialValue_EmptyString()
    {
        var logLevel = new LogLevel();

        var result = logLevel.AStar;

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void AStar_ShouldAllowSettingValue()
    {
        var logLevel      = new LogLevel();
        var expectedValue = "Error";

        logLevel.AStar = expectedValue;
        var result = logLevel.AStar;

        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public void ToString_ShouldListAllProperties()
    {
        var logLevel = new LogLevel { Default = "Debug", MicrosoftAspNetCore = "Information", AStar = "Error" };

        var result = logLevel.ToJson();

        result.ShouldMatchApproved();
    }

    [Fact]
    public void Equals_ShouldReturnTrueForIdenticalValues()
    {
        var logLevel1 = new LogLevel { Default = "Info", MicrosoftAspNetCore = "Debug", AStar = "Trace" };

        var logLevel2 = new LogLevel { Default = "Info", MicrosoftAspNetCore = "Debug", AStar = "Trace" };

        logLevel1.ToJson().ShouldBeEquivalentTo(logLevel2.ToJson());
    }

    [Fact]
    public void Equals_ShouldReturnFalseForDifferentValues()
    {
        var logLevel1 = new LogLevel { Default = "Info", MicrosoftAspNetCore = "Debug", AStar = "Trace" };

        var logLevel2 = new LogLevel { Default = "Warn", MicrosoftAspNetCore = "Error", AStar = "Fatal" };

        var areEqual = logLevel1.Equals(logLevel2);

        Assert.False(areEqual);
    }
}
