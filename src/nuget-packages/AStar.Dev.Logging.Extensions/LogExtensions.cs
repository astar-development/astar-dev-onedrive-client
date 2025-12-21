using Microsoft.Extensions.Logging;

namespace AStar.Dev.Logging.Extensions;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public class LogExtensions<T> : ILogger<T>
{
    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => throw new NotImplementedException();
    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => throw new NotImplementedException();
    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => throw new NotImplementedException();
}
