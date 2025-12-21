namespace AStar.Dev.Functional.Extensions;

/// <summary>
///     A set of small convenience extensions to make consuming Result{T, Exception}
///     values in view-models and tests a bit easier.
/// </summary>
public static class ConvenienceResultExtensions
{
    /// <summary>
    ///     Returns the contained value or throws the captured exception.
    /// </summary>
    public static T GetOrThrow<T>(this Result<T, Exception> result)
        => result switch
        {
            Result<T, Exception>.Ok ok => ok.Value,
            Result<T, Exception>.Error err => throw err.Reason,
            _ => throw new InvalidOperationException("Unrecognized Result variant")
        };

    /// <summary>
    ///     Awaits a task returning a Result and returns the value or throws the captured exception.
    /// </summary>
    public static async Task<T> GetOrThrowAsync<T>(this Task<Result<T, Exception>> resultTask)
        => (await resultTask.ConfigureAwait(false)).GetOrThrow();

    /// <summary>
    ///     Returns a short, UI-friendly error message for the Result. Empty string for success.
    /// </summary>
    public static string ToErrorMessage<T>(this Result<T, Exception> result)
        => result is Result<T, Exception>.Error err ? err.Reason?.Message ?? string.Empty : string.Empty;
}
