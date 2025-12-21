namespace AStar.Dev.Logging.Extensions;

[TestSubject(typeof(Configuration))]
public class ConfigurationShould
{
    [Fact]
    public void ExternalSettingsFile_ShouldReturn_DefaultFilename()
    {
        var expected = "astar-logging-settings.json";

        var result = Configuration.ExternalSettingsFile;

        result.ShouldBe(expected);
    }

    [Fact]
    public void ExternalSettingsFile_ShouldNotReturn_EmptyString()
    {
        var result = Configuration.ExternalSettingsFile;

        result.ShouldNotBeNullOrEmpty("ExternalSettingsFile should not be an empty string.");
    }

    [Fact]
    public void ExternalSettingsFile_ShouldNotReturn_Null()
    {
        var result = Configuration.ExternalSettingsFile;

        result.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("astar-logging-settings.json")]
    [InlineData("ASTAR-LOGGING-SETTINGS.JSON")] // Case-insensitivity check
    public void ExternalSettingsFile_ShouldMatch_ExpectedContentRegardlessOfCase(string comparisonValue)
    {
        var result = Configuration.ExternalSettingsFile;

        string.Equals(result, comparisonValue, StringComparison.OrdinalIgnoreCase)
              .ShouldBeTrue($"Expected {comparisonValue}, but got {result}.");
    }
}
