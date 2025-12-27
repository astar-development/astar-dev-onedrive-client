using AStar.Dev.Utilities;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(Extensions.Models.LogLevel))]
public class LogLevelShould
{
    [Fact]
    public void Default_ShouldHaveInitialValue_EmptyString()
    {
        var logLevel = new Extensions.Models.LogLevel();

        var result = logLevel.Default;

        result.ShouldNotBeNull();
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void Default_ShouldAllowSettingValue()
    {
        var logLevel      = new Extensions.Models.LogLevel();
        var expectedValue = "Info";

        logLevel.Default = expectedValue;
        var result = logLevel.Default;

        result.ShouldBe(expectedValue);
    }

    [Fact]
    public void MicrosoftAspNetCore_ShouldHaveInitialValue_EmptyString()
    {
        var logLevel = new Extensions.Models.LogLevel();

        var result = logLevel.MicrosoftAspNetCore;

        result.ShouldNotBeNull();
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void MicrosoftAspNetCore_ShouldAllowSettingValue()
    {
        var logLevel      = new Extensions.Models.LogLevel();
        var expectedValue = "Warning";

        logLevel.MicrosoftAspNetCore = expectedValue;
        var result = logLevel.MicrosoftAspNetCore;

        result.ShouldBe(expectedValue);
    }

    [Fact]
    public void AStar_ShouldHaveInitialValue_EmptyString()
    {
        var logLevel = new Extensions.Models.LogLevel();

        var result = logLevel.AStar;

        result.ShouldNotBeNull();
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void AStar_ShouldAllowSettingValue()
    {
        var logLevel      = new Extensions.Models.LogLevel();
        var expectedValue = "Error";

        logLevel.AStar = expectedValue;
        var result = logLevel.AStar;

        result.ShouldBe(expectedValue);
    }

    [Fact]
    public void ToString_ShouldListAllProperties()
    {
        var logLevel = new Extensions.Models.LogLevel { Default = "Debug", MicrosoftAspNetCore = "Information", AStar = "Error" };

        var result = logLevel.ToJson();

        result.ShouldMatchApproved();
    }

    [Fact]
    public void Equals_ShouldReturnTrueForIdenticalValues()
    {
        var logLevel1 = new Extensions.Models.LogLevel { Default = "Info", MicrosoftAspNetCore = "Debug", AStar = "Trace" };

        var logLevel2 = new Extensions.Models.LogLevel { Default = "Info", MicrosoftAspNetCore = "Debug", AStar = "Trace" };

        logLevel1.ToJson().ShouldBeEquivalentTo(logLevel2.ToJson());
    }

    [Fact]
    public void Equals_ShouldReturnFalseForDifferentValues()
    {
        var logLevel1 = new Extensions.Models.LogLevel { Default = "Info", MicrosoftAspNetCore = "Debug", AStar = "Trace" };

        var logLevel2 = new Extensions.Models.LogLevel { Default = "Warn", MicrosoftAspNetCore = "Error", AStar = "Fatal" };

        var areEqual = logLevel1.Equals(logLevel2);

        areEqual.ShouldBeFalse();
    }
}
