using System;
using System.Threading.Tasks;

namespace AStar.Dev.Functional.Extensions;

/// <summary>
///     Represents a computation that may succeed or throw an exception.
/// </summary>
#pragma warning disable CA1716
public static class Try
#pragma warning restore CA1716
{
    /// <summary>
    ///     Runs an action and returns a Result indicating success or the captured exception.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>A <see cref="Result{TSuccess, Exception}" /> whose success value is <c>true</c> when the action completes.</returns>
    public static Result<bool, Exception> Run(Action action)
    {
        try
        {
            action();
            return new Result<bool, Exception>.Ok(true);
        }
        catch(Exception ex)
        {
            return new Result<bool, Exception>.Error(ex);
        }
    }

    /// <summary>
    ///     Runs a function and returns a Result containing the success result or captured exception.
    /// </summary>
    /// <param name="func">The computation to execute.</param>
    /// <returns>A <see cref="Result{TSuccess, Exception}" /> result.</returns>
    public static Result<T, Exception> Run<T>(Func<T> func)
    {
        try
        {
            return new Result<T, Exception>.Ok(func());
        }
        catch(Exception ex)
        {
            return new Result<T, Exception>.Error(ex);
        }
    }

    /// <summary>
    ///     Runs an async action and returns a Result indicating success or the captured exception.
    /// </summary>
    /// <param name="action">The async action to execute.</param>
    /// <returns>A Task of <see cref="Result{TSuccess, Exception}" /> whose success value is <c>true</c> when the action completes.</returns>
    public static async Task<Result<bool, Exception>> RunAsync(Func<Task> action)
    {
        try
        {
            await action();
            return new Result<bool, Exception>.Ok(true);
        }
        catch(Exception ex)
        {
            return new Result<bool, Exception>.Error(ex);
        }
    }

    /// <summary>
    ///     Runs an async function and returns a Result containing the success result or captured exception.
    /// </summary>
    /// <param name="func">The async computation to execute.</param>
    /// <returns>A Task of <see cref="Result{TSuccess, Exception}" /> result.</returns>
    public static async Task<Result<T, Exception>> RunAsync<T>(Func<Task<T>> func)
    {
        try
        {
            return new Result<T, Exception>.Ok(await func());
        }
        catch(Exception ex)
        {
            return new Result<T, Exception>.Error(ex);
        }
    }
}
