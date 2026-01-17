namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Service for logging debug information to the database when enabled for an account.
/// </summary>
public interface IDebugLogger
{
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="source">The source of the log (typically class or method name).</param>
    /// <param name="message">The log message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogInfoAsync(string source, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="source">The source of the log (typically class or method name).</param>
    /// <param name="message">The log message.</param>
    /// <param name="exception">Optional exception details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogErrorAsync(string source, string message, Exception? exception = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a method entry.
    /// </summary>
    /// <param name="source">The source of the log (typically class.Method).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogEntryAsync(string source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a method exit.
    /// </summary>
    /// <param name="source">The source of the log (typically class.Method).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogExitAsync(string source, CancellationToken cancellationToken = default);
}
