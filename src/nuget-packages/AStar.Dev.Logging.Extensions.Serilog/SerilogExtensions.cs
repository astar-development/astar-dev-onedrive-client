using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace AStar.Dev.Logging.Extensions.Serilog;

/// <summary>
///     Provides extension methods to simplify the creation and configuration of Serilog loggers.
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    ///     Creates and configures a minimal Serilog logger with a debug-level log threshold that outputs to the console.
    /// </summary>
    /// <returns>An instance of <see cref="ILogger" /> configured with a minimal setup.</returns>
    public static ILogger CreateMinimalLogger() => new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console()
        .CreateBootstrapLogger();

    /// <summary>
    /// Configures a <see cref="LoggerConfiguration"/> with the default AStar settings and sinks.
    /// </summary>
    /// <param name="config">The logger configuration to update.</param>
    /// <param name="configuration">Application configuration to read Serilog settings from.</param>
    /// <param name="addFileSink">When true, adds a rolling file sink; set to false in unit tests to avoid real IO.</param>
    /// <returns>The same <see cref="LoggerConfiguration"/> for chaining.</returns>
    public static LoggerConfiguration ConfigureAStarDevelopmentLoggingDefaults(this LoggerConfiguration config, IConfiguration configuration, bool addFileSink = true)
    {
        LoggerConfiguration cfg = config
            .Enrich.FromLogContext()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information);

        IConfigurationSection serilogSection = configuration.GetSection("Serilog");
        if(serilogSection.Exists())
            cfg = cfg.ReadFrom.Configuration(configuration);

        return addFileSink ? ConfigureFileSink(cfg) : cfg;
    }

    private static LoggerConfiguration ConfigureFileSink(LoggerConfiguration config)
    {
        try
        {
            var baseDir = OperatingSystem.IsWindows()
                ? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                : Environment.GetEnvironmentVariable("XDG_STATE_HOME")
                          ?? Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
                          ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");

            var logDir = Path.Combine(baseDir, "astar-dev", "astar-dev-onedrive-client", "logs");
            _ = Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "astar-dev-onedrive-client-.log");

            return config.WriteTo.File(logPath, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Debug);
        }
        catch
        {
            return config;
        }
    }
}
