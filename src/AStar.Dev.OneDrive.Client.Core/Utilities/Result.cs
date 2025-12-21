namespace AStar.Dev.OneDrive.Client.Core.Utilities;

/// <summary>
/// Minimal Result type for simple success/failure flows.
/// Keep small and focused; expand in services if needed.
/// </summary>
public readonly record struct Result(bool IsSuccess, string? Error)
{
    public static Result Success => new(true, null);
    public static Result Failure(string error) => new(false, error);
}
