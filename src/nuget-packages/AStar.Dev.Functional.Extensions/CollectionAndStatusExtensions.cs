using System.Collections.ObjectModel;

namespace AStar.Dev.Functional.Extensions;

/// <summary>
///     Helpers for applying Result&lt;IEnumerable&lt;T&gt;, Exception&gt; to UI collections
///     and mapping Result to short status messages.
/// </summary>
public static class CollectionAndStatusExtensions
{
    /// <summary>
    ///     Awaits a task that returns a Result of an enumerable and replaces the contents of
    ///     the target collection on success, or invokes the onError handler on failure.
    /// </summary>
    public static async Task ApplyToCollectionAsync<T>(this Task<Result<IEnumerable<T>, Exception>> resultTask, ObservableCollection<T> target, Action<Exception>? onError = null)
    {
        Result<IEnumerable<T>, Exception>? result = await resultTask.ConfigureAwait(false);
        switch(result)
        {
            case Result<IEnumerable<T>, Exception>.Ok ok:
            {
                target.Clear();
                foreach(T item in ok.Value ?? [])
                    target.Add(item);

                break;
            }
            case Result<IEnumerable<T>, Exception>.Error err:
                onError?.Invoke(err.Reason);
                break;
        }
    }

    /// <summary>
    ///     Maps a Result to a human-friendly status string. If the result is a success, calls
    ///     successFormatter; if error, returns the error message (or uses errorFormatter if provided).
    /// </summary>
    public static string ToStatus<T>(this Result<T, Exception> result, Func<T, string> successFormatter, Func<Exception, string>? errorFormatter = null)
        => result switch
        {
            Result<T, Exception>.Ok ok => successFormatter(ok.Value),
            Result<T, Exception>.Error err => (errorFormatter ?? (e => e.Message))(err.Reason),
            _ => string.Empty
        };
}
