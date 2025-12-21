using Microsoft.Extensions.Logging;

namespace AStar.Dev.Logging.Extensions;

/// <summary>
/// Provides logging functionality for AStar applications, extending the generic <see cref="ILogger{TCategoryName}"/> interface.
/// </summary>
/// <typeparam name="T">
/// The type whose name is used for the logger category. Typically the class or interface requesting logging.
/// </typeparam>
public interface ILoggerAstar<out T> : ILogger<T>
{
    /// <summary>
    /// Logs a page view event for the specified page name.
    /// </summary>
    /// <param name="pageName">
    /// The name of the page that was viewed. This value is used for analytics and diagnostic purposes.
    /// </param>
    void LogPageView(string pageName);
}
