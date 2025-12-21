using AStar.Dev.Logging.Extensions.Serilog;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace AStar.Dev.Utilities.Serilog.Tests.Unit;

public class SerilogExtensionsShould
{
    [Fact]
    public void CreateMinimalLoggerWhichWritesToConsoleAndHonoursTheMinimumLogLevelOfDebug()
    {

        ILogger logger = SerilogExtensions.CreateMinimalLogger();
        var verboseToken = $"VERBOSE-{Guid.NewGuid():N}";
        var debugToken = $"DEBUG-{Guid.NewGuid():N}";
        var infoToken = $"INFO-{Guid.NewGuid():N}";

        TextWriter originalOut = Console.Out;
        using var capture = new StringWriter();

        try
        {
            Console.SetOut(capture);

            logger.Verbose("{Token}", verboseToken);
            logger.Debug("{Token}", debugToken);
            logger.Information("{Token}", infoToken);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var output = capture.ToString();

        _ = output.ShouldNotBeNull();
        output.ShouldContain(debugToken);
        output.ShouldContain(infoToken);
        output.ShouldNotContain(verboseToken);
    }

    [Fact]
    public void ConfigureAStarDevelopmentLoggingDefaultsCanConfigureWithoutFileSink()
    {
        Microsoft.Extensions.Configuration.IConfiguration cfg = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        var loggerConfig = new LoggerConfiguration();
        _ = loggerConfig.ConfigureAStarDevelopmentLoggingDefaults(cfg, addFileSink: false);
        ILogger logger = loggerConfig.CreateLogger();

        var token = $"NOFILE-{Guid.NewGuid():N}";
        Should.NotThrow(() => logger.Information("{Token}", token));
    }

    [Fact]
    public void ConfigureAStarDevelopmentLoggingDefaultsOverridesMicrosoftToInformation()
    {
        IConfiguration cfg = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        var loggerConfig = new LoggerConfiguration();
        _ = loggerConfig.ConfigureAStarDevelopmentLoggingDefaults(cfg, addFileSink: false);
        ILogger logger = loggerConfig.CreateLogger();

        var infoToken = $"MS-I-{Guid.NewGuid():N}";
        ILogger microsoftLogger = logger.ForContext("SourceContext", "Microsoft");
        Should.NotThrow(() => microsoftLogger.Information("{Token}", infoToken));
    }
}
