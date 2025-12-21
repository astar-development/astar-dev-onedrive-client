using AStar.Dev.Logging.Extensions.EventIds;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.Logging.Extensions;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TCategoryName"></typeparam>
public sealed class AStarLogger<TCategoryName> : ILoggerAstar<TCategoryName>
{
    private readonly ILogger<TCategoryName> _logger;
    private readonly ITelemetryClient _telemetryClient;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable IDE0290 // Use primary constructor
#pragma warning disable S6672 // Generic logger injection should match enclosing type
    public AStarLogger(ILogger<TCategoryName> logger, ITelemetryClient telemetryClient)
#pragma warning restore S6672 // Generic logger injection should match enclosing type
#pragma warning restore IDE0290 // Use primary constructor
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        _logger = logger;
        _telemetryClient = telemetryClient;
    }
    /// <inheritdoc />
    public void LogPageView(string pageName)
    {
        ArgumentNullException.ThrowIfNull(pageName);

#pragma warning disable CA1848 // Use the LoggerMessage delegates
        _logger.LogInformation(AStarEventIds.Website.PageView, "Page view: {PageView}", pageName);
#pragma warning restore CA1848 // Use the LoggerMessage delegates
        _telemetryClient.TrackPageView(pageName);
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _logger.BeginScope(state);

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => _logger.Log(logLevel, eventId, state, exception, formatter);
}
