namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Static facade for convenient debug logging access throughout the application.
/// Provides static methods that delegate to the singleton IDebugLogger instance.
/// </summary>
public static class DebugLog
{
    private static IDebugLogger? _instance;

    /// <summary>
    /// Initializes the static logger instance. Should be called once during application startup.
    /// </summary>
    /// <param name="logger">The IDebugLogger instance to use.</param>
    public static void Initialize(IDebugLogger logger) => _instance = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="source">The source of the log (typically class or method name).</param>
    /// <param name="message">The log message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static Task InfoAsync(string source, string message, CancellationToken cancellationToken = default) => _instance?.LogInfoAsync(source, message, cancellationToken) ?? Task.CompletedTask;

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="source">The source of the log (typically class or method name).</param>
    /// <param name="message">The log message.</param>
    /// <param name="exception">Optional exception details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static Task ErrorAsync(string source, string message, Exception? exception = null, CancellationToken cancellationToken = default) => _instance?.LogErrorAsync(source, message, exception, cancellationToken) ?? Task.CompletedTask;

    /// <summary>
    /// Logs a method entry.
    /// </summary>
    /// <param name="source">The source of the log (typically class.Method).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static Task EntryAsync(string source, CancellationToken cancellationToken = default) => _instance?.LogEntryAsync(source, cancellationToken) ?? Task.CompletedTask;

    /// <summary>
    /// Logs a method exit.
    /// </summary>
    /// <param name="source">The source of the log (typically class.Method).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static Task ExitAsync(string source, CancellationToken cancellationToken = default) => _instance?.LogExitAsync(source, cancellationToken) ?? Task.CompletedTask;
}
