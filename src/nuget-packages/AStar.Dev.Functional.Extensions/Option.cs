namespace AStar.Dev.Functional.Extensions;

/// <summary>
///     Factory methods for creating instances of <see cref="Option{T}" />.
/// </summary>
#pragma warning disable CA1716
public static class Option
#pragma warning restore CA1716
{
    /// <summary>
    ///     Creates a <c>Some</c> instance containing the specified non-null value.
    /// </summary>
    public static Option<T> Some<T>(T value) => new Option<T>.Some(value);

    /// <summary>
    ///     Returns a <c>None</c> instance representing an absent value.
    /// </summary>
    public static Option<T> None<T>() => Option<T>.None.Instance;
}
