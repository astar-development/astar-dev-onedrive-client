using AStar.Dev.Logging.Extensions.Models;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(Extensions.Models.Serilog))]
public class SerilogShould
{
    [Fact]
    public void SetTheEnrichPropertyToAnEmptyArrayByDefault()
    {
        var serilog = new Extensions.Models.Serilog();

        var enrich = serilog.Enrich;

        Assert.NotNull(enrich);
        Assert.Empty(enrich);
    }

    [Fact]
    public void SetTheEnrichPropertyToTheProvidedValues()
    {
        var serilog       = new Extensions.Models.Serilog();
        var testEnrichers = new[] { "Enricher1", "Enricher2" };

        serilog.Enrich = testEnrichers;

        Assert.Equal(testEnrichers, serilog.Enrich);
    }

    [Fact]
    public void SetTheWriteToPropertyToAnEmptyArrayByDefault()
    {
        var serilog = new Extensions.Models.Serilog();

        WriteTo[] writeTo = serilog.WriteTo;

        Assert.NotNull(writeTo);
        Assert.NotEmpty(writeTo);
    }

    [Fact]
    public void SetTheWriteToPropertyToTheProvidedValues()
    {
        var serilog = new Extensions.Models.Serilog();

        WriteTo[] writeToConfigs = [new WriteTo { Name = "Console", Args = new() { ServerUrl = "http://localhost" } }, new WriteTo { Name = "File", Args = new() { ServerUrl = "C:\\Logs" } }];

        serilog.WriteTo = writeToConfigs;

        Assert.Equal(writeToConfigs, serilog.WriteTo);
    }

    [Fact]
    public void SetTheMinimumLevelPropertyToAnEmptyInstanceByDefault()
    {
        var serilog = new Extensions.Models.Serilog();

        MinimumLevel minimumLevel = serilog.MinimumLevel;

        Assert.NotNull(minimumLevel);
        Assert.Equal(string.Empty, minimumLevel.Default);
        Assert.NotNull(minimumLevel.Override);
    }

    [Fact]
    public void SetTheMinimumLevelPropertyToTheProvidedValues()
    {
        var serilog              = new Extensions.Models.Serilog();
        var modifiedMinimumLevel = new MinimumLevel { Default = "Error", Override = new() { MicrosoftAspNetCore = "Warning", SystemNetHttp = "Information", AStar = "Debug" } };

        serilog.MinimumLevel = modifiedMinimumLevel;

        Assert.Equal(modifiedMinimumLevel, serilog.MinimumLevel);
        Assert.Equal("Error", serilog.MinimumLevel.Default);
        Assert.Equal("Warning", serilog.MinimumLevel.Override.MicrosoftAspNetCore);
        Assert.Equal("Information", serilog.MinimumLevel.Override.SystemNetHttp);
        Assert.Equal("Debug", serilog.MinimumLevel.Override.AStar);
    }
}
