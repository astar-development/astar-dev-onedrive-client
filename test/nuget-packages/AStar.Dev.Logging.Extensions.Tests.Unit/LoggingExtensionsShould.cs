using AStar.Dev.Logging.Extensions.Models;
using AStar.Dev.Utilities;

namespace AStar.Dev.Logging.Extensions;

public sealed class LoggingExtensionsShould
{
    [Theory]
    [InlineData("This is not a valid filename for a lot of reasons")]
    [InlineData(@"c:\This is not a valid filename\as the path\and filename\do not exist.what.did.you.expect.lol")]
    public void ThrowExceptionWhenAddSerilogLoggingIsCalledButConfigIsntValid(string? fileNameWithPath)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        void action()
        {
            builder.AddSerilogLogging(fileNameWithPath!);
        }

        Should.NotThrow(action);
    }

    [Fact]
    public void AddTheExpectedNumberOfSerilogServices()
    {
        WebApplicationBuilder builder              = WebApplication.CreateBuilder();
        const int minimumNumberOfExpectedServices = 4;
        var       testConfig           = new SerilogConfig { Serilog = { WriteTo = [new() { Args = new() { ServerUrl = "https://example.com" } }], MinimumLevel = new (){ Default = "Information"} } };

        File.WriteAllText("serilog.config", testConfig.ToJson()); // OK, not a true unit test but...
        var serviceCount = builder.Services.Count;
        WebApplicationBuilder sut = builder.AddSerilogLogging("serilog.config");

        sut.Services.Where(d => d.ServiceType.Assembly.FullName?.StartsWith("Serilog") == true).Count().ShouldBeGreaterThanOrEqualTo(minimumNumberOfExpectedServices);
    }
}
