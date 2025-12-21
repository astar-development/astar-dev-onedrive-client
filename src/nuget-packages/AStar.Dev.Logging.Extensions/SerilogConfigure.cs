using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace AStar.Dev.Logging.Extensions;

/// <summary>
/// Provides extension methods for configuring Serilog logging with Application Insights and console output.
/// </summary>
internal static class SerilogConfigure
{
    /// <summary>
    /// Configures the Serilog logger to write logs to Application Insights and the console, and reads additional settings from the specified configuration.
    /// </summary>
    /// <param name="loggerConfiguration">The Serilog <see cref="LoggerConfiguration"/> to configure.</param>
    /// <param name="configuration">The application configuration containing Serilog settings.</param>
    /// <param name="telemetryConfiguration">The Application Insights telemetry configuration.</param>
    /// <returns>The configured <see cref="LoggerConfiguration"/> instance.</returns>
    public static LoggerConfiguration Configure(this LoggerConfiguration loggerConfiguration, IConfiguration configuration, TelemetryConfiguration telemetryConfiguration)
#pragma warning disable CA1305 // Specify IFormatProvider
        => loggerConfiguration
            .WriteTo.ApplicationInsights(telemetryConfiguration,
                                         TelemetryConverter.Traces)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {Message:lj}{NewLine}{Exception}")
            .ReadFrom.Configuration(configuration);
#pragma warning restore CA1305 // Specify IFormatProvider
}
