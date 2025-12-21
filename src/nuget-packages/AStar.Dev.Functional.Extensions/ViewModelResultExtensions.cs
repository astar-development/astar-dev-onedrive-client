namespace AStar.Dev.Functional.Extensions;

/// <summary>
///     Helper extensions to simplify consuming <see cref="Result{TSuccess, TError}" /> in view-models.
/// </summary>
public static class ViewModelResultExtensions
{
    /// <summary>
    ///     Applies a Result to onSuccess/onError handlers.
    /// </summary>
    public static void Apply<T>(this Result<T, Exception> result, Action<T> onSuccess, Action<Exception>? onError = null)
    {
        switch(result)
        {
            case Result<T, Exception>.Ok ok:
                onSuccess(ok.Value);
                return;
            case Result<T, Exception>.Error err:
                onError?.Invoke(err.Reason);
                break;
        }
    }

    /// <summary>
    ///     Awaits a task Result and applies handlers.
    /// </summary>
    public static async Task ApplyAsync<T>(this Task<Result<T, Exception>> resultTask, Action<T> onSuccess, Action<Exception>? onError = null)
    {
        Result<T, Exception>? result = await resultTask.ConfigureAwait(false);
        result.Apply(onSuccess, onError);
    }

    /// <summary>
    ///     Awaits a task Result and applies async handlers.
    /// </summary>
    public static async Task ApplyAsync<T>(this Task<Result<T, Exception>> resultTask, Func<T, Task> onSuccessAsync, Func<Exception, Task>? onErrorAsync = null)
    {
        Result<T, Exception>? result = await resultTask.ConfigureAwait(false);
        switch(result)
        {
            case Result<T, Exception>.Ok ok:
                await onSuccessAsync(ok.Value).ConfigureAwait(false);
                return;
            case Result<T, Exception>.Error err when onErrorAsync is not null:
                await onErrorAsync(err.Reason).ConfigureAwait(false);
                break;
        }
    }
}
